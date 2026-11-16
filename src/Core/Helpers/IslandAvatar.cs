using System;
using System.Globalization;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Helpers;

/// <summary>
/// Renders the run's character portrait into a small image the island can carry.
/// <para>
/// A Live Activity renders inside the widget extension, which has its own sandbox - it cannot read
/// Godot's <c>user://</c> directory, and this port is signed ad hoc so there is no App Group to
/// share one. The picture therefore travels inside the activity's content state: this writes the
/// image next to <c>island_state.json</c>, <c>Sts2IslandBridge.swift</c> reads the bytes and puts
/// them in <c>ContentState.avatar</c>, and the widget decodes them with <c>UIImage(data:)</c>.
/// </para>
/// <para>
/// ActivityKit archives attributes and content state together and refuses the whole request when
/// the result exceeds 4 KB (<c>ActivityAuthorizationError.attributesTooLarge</c>). A 132 px PNG of a
/// painted portrait lands well past 10 KB, so the old 48 KB budget meant every request threw and no
/// island ever appeared. The budget here is <see cref="BudgetBytes" />, chosen to leave room for the
/// struct's own fields plus ActivityKit's archiving overhead, and the encoder walks a ladder of
/// sizes and qualities until the file fits - JPEG first, because a photograph at 96 px is a couple
/// of kilobytes where the same picture as PNG is not.
/// </para>
/// <para>
/// Encoding happens once per character, not once per poll: switching heroes rewrites the file, and
/// a failed character is not retried so a bad asset cannot turn into a per-second encode.
/// </para>
/// </summary>
public static class IslandAvatar
{
	/// <summary>
	/// File name stem inside <c>user://</c>. The published name carries the real extension, because
	/// the encoder switches between PNG and JPEG depending on which one fits the budget.
	/// </summary>
	private const string Stem = "island_avatar";

	/// <summary>
	/// Ceiling for the encoded image. ActivityKit's hard limit is 4096 bytes for the archived
	/// attributes plus content state; the struct's integers, the character name and the archiving
	/// envelope take a few hundred, so the picture gets 3000.
	/// </summary>
	private const int BudgetBytes = 3000;

	/// <summary>Largest edge we ever encode: 3x for the 44 pt lock-screen slot.</summary>
	private const int MaxEdge = 132;

	/// <summary>Below this the portrait is a smear; publish nothing and let the widget use its symbol.</summary>
	private const int MinEdge = 48;

	/// <summary>
	/// Edges to try, in order. The island's leading slot is 30 pt and the compact slot 17 pt, so
	/// anything at or above 96 px is already sharper than the space it is drawn into.
	/// </summary>
	private static readonly int[] Edges = { 132, 112, 96, 80, 64, 48 };

	/// <summary>JPEG qualities to try at each edge, best first.</summary>
	private static readonly float[] Qualities = { 0.85f, 0.7f, 0.55f, 0.4f, 0.28f };

	private static string _encodedFor = string.Empty;

	private static bool _unavailable;

	/// <summary>
	/// Makes sure the portrait file next to <c>island_state.json</c> matches
	/// <paramref name="character" /> and returns the file name to publish, or an empty string when
	/// there is no usable portrait (the widget then falls back to its symbol).
	/// </summary>
	public static string Ensure(CharacterModel? character)
	{
		if (character == null)
		{
			return string.Empty;
		}
		string id;
		try
		{
			id = character.Id.Entry.ToLowerInvariant();
		}
		catch (Exception)
		{
			return string.Empty;
		}
		if (id == _encodedFor)
		{
			return _unavailable ? string.Empty : _publishedName;
		}
		_encodedFor = id;
		_unavailable = false;
		_publishedName = string.Empty;
		try
		{
			_publishedName = Encode(character) ?? string.Empty;
			if (_publishedName.Length == 0)
			{
				_unavailable = true;
			}
			return _publishedName;
		}
		catch (Exception ex)
		{
			_unavailable = true;
			_publishedName = string.Empty;
			Log.Warn($"[Island] no avatar for '{id}': {ex.Message}");
			return string.Empty;
		}
	}

	private static string _publishedName = string.Empty;

	/// <summary>
	/// Walks the size ladder and returns the file name of the first encoding that fits
	/// <see cref="BudgetBytes" />, or null when nothing fits.
	/// </summary>
	private static string? Encode(CharacterModel character)
	{
		Image? source = LoadPortrait(character);
		if (source == null)
		{
			Log.Warn($"[Island] '{character.Id.Entry}' has no readable portrait texture");
			return null;
		}

		// JPEG first: the lock-screen art is a painting, and PNG of a painting is enormous.
		foreach (int edge in Edges)
		{
			if (edge < MinEdge)
			{
				continue;
			}
			Image scaled = Fit(source, edge);
			foreach (float quality in Qualities)
			{
				if (TryWrite(scaled, edge, quality, jpeg: true, out string name, out long bytes))
				{
					Log.Info($"[Island] avatar for '{character.Id.Entry}': {scaled.GetWidth()}x{scaled.GetHeight()} jpeg q={quality.ToString(CultureInfo.InvariantCulture)}, {bytes.ToString(CultureInfo.InvariantCulture)} bytes");
					return name;
				}
			}
		}

		// Then PNG, which wins only for flat artwork with large areas of one colour.
		foreach (int edge in Edges)
		{
			if (edge < MinEdge)
			{
				continue;
			}
			Image scaled = Fit(source, edge);
			if (TryWrite(scaled, edge, 0f, jpeg: false, out string name, out long bytes))
			{
				Log.Info($"[Island] avatar for '{character.Id.Entry}': {scaled.GetWidth()}x{scaled.GetHeight()} png, {bytes.ToString(CultureInfo.InvariantCulture)} bytes");
				return name;
			}
		}

		DeleteAll();
		Log.Warn($"[Island] avatar for '{character.Id.Entry}' will not fit {BudgetBytes} bytes at any size");
		return null;
	}

	/// <summary>
	/// Encodes one candidate into memory and, if it fits <see cref="BudgetBytes" />, commits it to
	/// disk. Encoding to a buffer first keeps the ladder from leaving half-tried files behind.
	/// </summary>
	private static bool TryWrite(Image image, int edge, float quality, bool jpeg, out string name, out long bytes)
	{
		name = string.Empty;
		bytes = 0L;
		byte[]? buffer;
		try
		{
			buffer = jpeg ? image.SaveJpgToBuffer(quality) : image.SavePngToBuffer();
		}
		catch (Exception ex)
		{
			Log.Warn($"[Island] avatar encode failed at {edge}px ({(jpeg ? "jpeg" : "png")}): {ex.Message}");
			return false;
		}
		if (buffer == null || buffer.Length == 0)
		{
			return false;
		}
		if (buffer.Length > BudgetBytes)
		{
			return false;
		}
		string candidate = jpeg ? Stem + ".jpg" : Stem + ".png";
		try
		{
			DeleteAll();
			File.WriteAllBytes(AbsolutePath(candidate), buffer);
		}
		catch (Exception ex)
		{
			Log.Warn($"[Island] avatar write failed: {ex.Message}");
			return false;
		}
		name = candidate;
		bytes = buffer.Length;
		return true;
	}

	/// <summary>
	/// The full-body character-select art first (that is the "2D 形象"), the small top-panel icon as
	/// a fallback. Both live in the pack as VRAM-compressed textures, so the CPU pixels have to be
	/// decompressed before they can be resized or encoded.
	/// </summary>
	private static Image? LoadPortrait(CharacterModel character)
	{
		Image? image = Pixels(character.CharacterSelectIcon);
		if (image != null)
		{
			return image;
		}
		return Pixels(character.IconTexture);
	}

	private static Image? Pixels(Texture2D? texture)
	{
		if (texture == null)
		{
			return null;
		}
		Image? image = texture.GetImage();
		if (image == null || image.IsEmpty())
		{
			return null;
		}
		if (image.IsCompressed())
		{
			Error error = image.Decompress();
			if (error != Error.Ok)
			{
				return null;
			}
		}
		return image.IsEmpty() ? null : image;
	}

	/// <summary>
	/// Scales the long edge down to <paramref name="edge" />, never up. The island draws the picture
	/// in a circle, so the frame is squared off around the centre first: a tall full-body portrait
	/// squeezed into a circle otherwise arrives as a sliver of legs.
	/// </summary>
	private static Image Fit(Image source, int edge)
	{
		Image image = (Image)source.Duplicate();
		if (image.GetFormat() != Image.Format.Rgba8)
		{
			image.Convert(Image.Format.Rgba8);
		}
		int width = image.GetWidth();
		int height = image.GetHeight();
		if (width <= 0 || height <= 0)
		{
			return image;
		}
		int side = Math.Min(width, height);
		if (side > edge)
		{
			// Crop to a square around the middle, biased upward: portraits carry faces in the
			// upper half, so a centred square cuts the head off.
			int left = Math.Max(0, (width - side) / 2);
			int top = Math.Max(0, (int)((height - side) * 0.25f));
			image = image.GetRegion(new Rect2I(left, top, side, side));
		}
		int longEdge = Math.Max(image.GetWidth(), image.GetHeight());
		if (longEdge > edge)
		{
			float scale = (float)edge / longEdge;
			int targetWidth = Math.Max(1, (int)MathF.Round(image.GetWidth() * scale));
			int targetHeight = Math.Max(1, (int)MathF.Round(image.GetHeight() * scale));
			image.Resize(targetWidth, targetHeight, Image.Interpolation.Lanczos);
		}
		return image;
	}

	private static string UserPath(string name)
	{
		return "user://" + name;
	}

	private static string AbsolutePath(string name)
	{
		return Path.Combine(OS.GetUserDataDir(), name);
	}

	/// <summary>Removes both possible extensions so a stale PNG cannot shadow a fresh JPEG.</summary>
	private static void DeleteAll()
	{
		Delete(UserPath(Stem + ".png"));
		Delete(UserPath(Stem + ".jpg"));
	}

	private static void Delete(string path)
	{
		try
		{
			string absolute = ProjectSettings.GlobalizePath(path);
			if (File.Exists(absolute))
			{
				File.Delete(absolute);
			}
		}
		catch (Exception)
		{
			// Nothing to clean up if the file never landed.
		}
	}
}
