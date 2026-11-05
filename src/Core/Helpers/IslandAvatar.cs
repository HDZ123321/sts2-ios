using System;
using System.Globalization;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace MegaCrit.Sts2.Core.Helpers;

/// <summary>
/// Renders the run's character portrait into a small PNG the island can carry.
/// <para>
/// A Live Activity renders inside the widget extension, which has its own sandbox - it cannot read
/// Godot's <c>user://</c> directory, and this port is signed ad hoc so there is no App Group to
/// share one. The picture therefore travels inside the activity's content state: this writes the
/// PNG next to <c>island_state.json</c>, <c>Sts2IslandBridge.swift</c> reads the bytes and puts them
/// in <c>ContentState.avatar</c>, and the widget decodes them with <c>UIImage(data:)</c>.
/// </para>
/// <para>
/// The content state is archived and shipped to the widget on every update, so the image has to
/// stay small: 132 px on the long edge (3x for a 44 pt slot) and under 48 KB. When the portrait
/// will not fit - a texture the renderer refuses to hand back, a PNG that stays too heavy - the
/// reporter publishes no avatar and the widget falls back to its symbol.
/// </para>
/// <para>
/// Encoding happens once per character, not once per poll: switching heroes rewrites the file, and
/// a failed character is not retried so a bad asset cannot turn into a per-second encode.
/// </para>
/// </summary>
public static class IslandAvatar
{
	/// <summary>File name inside <c>user://</c>; the Swift bridge joins it onto the state directory.</summary>
	public const string FileName = "island_avatar.png";

	/// <summary>Long edge of the encoded portrait, in pixels. 132 = 3x for the 44 pt island slot.</summary>
	private const int MaxEdge = 132;

	/// <summary>Smallest edge we are willing to publish; below this the picture is not worth sending.</summary>
	private const int MinEdge = 72;

	/// <summary>Ceiling for the encoded PNG. Content state rides in every update, so keep it tight.</summary>
	private const int MaxBytes = 48 * 1024;

	private static string _encodedFor = string.Empty;

	private static bool _unavailable;

	/// <summary>
	/// Makes sure <c>user://island_avatar.png</c> matches <paramref name="character" /> and returns
	/// the file name to publish, or an empty string when there is no usable portrait.
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
			return _unavailable ? string.Empty : FileName;
		}
		_encodedFor = id;
		_unavailable = false;
		try
		{
			if (!Encode(character))
			{
				_unavailable = true;
				return string.Empty;
			}
			return FileName;
		}
		catch (Exception ex)
		{
			_unavailable = true;
			Log.Warn($"[Island] no avatar for '{id}': {ex.Message}");
			return string.Empty;
		}
	}

	private static bool Encode(CharacterModel character)
	{
		Image? source = LoadPortrait(character);
		if (source == null)
		{
			Log.Warn($"[Island] '{character.Id.Entry}' has no readable portrait texture");
			return false;
		}
		int edge = MaxEdge;
		while (edge >= MinEdge)
		{
			Image scaled = Fit(source, edge);
			Error error = scaled.SavePng(UserPath());
			if (error != Error.Ok)
			{
				Log.Warn($"[Island] avatar write failed: {error}");
				return false;
			}
			long bytes = new FileInfo(AbsolutePath()).Length;
			if (bytes <= MaxBytes)
			{
				Log.Info($"[Island] avatar for '{character.Id.Entry}': {scaled.GetWidth()}x{scaled.GetHeight()}, {bytes.ToString(CultureInfo.InvariantCulture)} bytes");
				return true;
			}
			edge /= 2;
		}
		Delete(UserPath());
		Log.Warn($"[Island] avatar for '{character.Id.Entry}' stays over {MaxBytes} bytes even at {MinEdge}px");
		return false;
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

	/// <summary>Scales the long edge down to <paramref name="edge" />, never up, and lands on RGBA8.</summary>
	private static Image Fit(Image source, int edge)
	{
		Image image = (Image)source.Duplicate();
		if (image.GetFormat() != Image.Format.Rgba8)
		{
			image.Convert(Image.Format.Rgba8);
		}
		int width = image.GetWidth();
		int height = image.GetHeight();
		int longEdge = Math.Max(width, height);
		if (longEdge <= edge)
		{
			return image;
		}
		float scale = (float)edge / longEdge;
		int targetWidth = Math.Max(1, (int)MathF.Round(width * scale));
		int targetHeight = Math.Max(1, (int)MathF.Round(height * scale));
		image.Resize(targetWidth, targetHeight, Image.Interpolation.Lanczos);
		return image;
	}

	private static string UserPath()
	{
		return "user://" + FileName;
	}

	private static string AbsolutePath()
	{
		return Path.Combine(OS.GetUserDataDir(), FileName);
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
