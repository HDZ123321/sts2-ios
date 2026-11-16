using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Saves;

namespace MegaCrit.Sts2.Core.Platform.Null;

public class NullPlatformUtilStrategy : IPlatformUtilStrategy
{
	private const string _multiplayerNamesFile = "mp_names.json";

	private readonly List<NullMultiplayerName>? _mpNames;

	public ulong LocalPlayerId { get; } = 1uL;

	public bool SupportsInviteDialog => false;

	public bool IsPlatformOverlayOpen => false;

	public NullPlatformUtilStrategy()
	{
		if (CommandLineHelper.TryGetValue("clientId", out string value) && ulong.TryParse(value, out var result))
		{
			LocalPlayerId = result;
		}
		GodotFileIo godotFileIo = new GodotFileIo(".");
		if (godotFileIo.FileExists("mp_names.json"))
		{
			string json = godotFileIo.ReadFile("mp_names.json");
			_mpNames = JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<List<NullMultiplayerName>>());
		}
	}

	public string GetPlayerName(ulong playerId)
	{
		if (_mpNames != null)
		{
			foreach (NullMultiplayerName mpName in _mpNames)
			{
				if (mpName.netId == playerId)
				{
					return mpName.name;
				}
			}
		}
		return playerId switch
		{
			1uL => "Test Host", 
			1000uL => "Test Client 1", 
			2000uL => "Test Client 2", 
			3000uL => "Test Client 3", 
			_ => playerId.ToString(), 
		};
	}

	public ulong GetLocalPlayerId()
	{
		return LocalPlayerId;
	}

	public Task<IEnumerable<ulong>> GetFriendsWithOpenLobbies()
	{
		return Task.FromResult((IEnumerable<ulong>)Array.Empty<ulong>());
	}

	public bool TryOpenInviteDialog(INetGameService netService)
	{
		return false;
	}

	public void OpenUrl(string url)
	{
		OS.ShellOpen(url);
	}

	public void OpenVirtualKeyboard()
	{
		if (!DisplayServer.HasFeature(DisplayServer.Feature.VirtualKeyboard))
		{
			return;
		}
		Control? focused = (Engine.GetMainLoop() as SceneTree)?.Root?.GuiGetFocusOwner();
		if (focused is LineEdit lineEdit)
		{
			DisplayServer.VirtualKeyboardShow(lineEdit.Text, lineEdit.GetGlobalRect(), ToDisplayServerKeyboardType(lineEdit.VirtualKeyboardType), lineEdit.MaxLength, lineEdit.CaretColumn);
		}
		else if (focused is TextEdit textEdit)
		{
			DisplayServer.VirtualKeyboardShow(textEdit.Text, textEdit.GetGlobalRect(), DisplayServer.VirtualKeyboardType.Multiline, -1, GetTextEditCaretOffset(textEdit), GetTextEditCaretEnd(textEdit));
		}
		else
		{
			Log.Warn($"OpenVirtualKeyboard called with no text control focused ({focused?.GetType().Name ?? "null"}).");
		}
	}

	public void CloseVirtualKeyboard()
	{
		if (DisplayServer.HasFeature(DisplayServer.Feature.VirtualKeyboard))
		{
			DisplayServer.VirtualKeyboardHide();
		}
	}

	private static DisplayServer.VirtualKeyboardType ToDisplayServerKeyboardType(LineEdit.VirtualKeyboardTypeEnum type)
	{
		return type switch
		{
			LineEdit.VirtualKeyboardTypeEnum.Number => DisplayServer.VirtualKeyboardType.Number,
			LineEdit.VirtualKeyboardTypeEnum.NumberDecimal => DisplayServer.VirtualKeyboardType.NumberDecimal,
			LineEdit.VirtualKeyboardTypeEnum.Phone => DisplayServer.VirtualKeyboardType.Phone,
			LineEdit.VirtualKeyboardTypeEnum.EmailAddress => DisplayServer.VirtualKeyboardType.EmailAddress,
			LineEdit.VirtualKeyboardTypeEnum.Password => DisplayServer.VirtualKeyboardType.Password,
			LineEdit.VirtualKeyboardTypeEnum.Url => DisplayServer.VirtualKeyboardType.Url,
			LineEdit.VirtualKeyboardTypeEnum.Multiline => DisplayServer.VirtualKeyboardType.Multiline,
			_ => DisplayServer.VirtualKeyboardType.Default,
		};
	}

	private static int GetTextEditCaretOffset(TextEdit textEdit)
	{
		int caretLine = textEdit.GetCaretLine();
		int offset = 0;
		for (int i = 0; i < caretLine; i++)
		{
			offset += textEdit.GetLine(i).Length + 1;
		}
		return offset + textEdit.GetCaretColumn();
	}

	private static int GetTextEditCaretEnd(TextEdit textEdit)
	{
		if (!textEdit.HasSelection())
		{
			return -1;
		}
		int line = textEdit.GetSelectionToLine();
		int offset = 0;
		for (int i = 0; i < line; i++)
		{
			offset += textEdit.GetLine(i).Length + 1;
		}
		return offset + textEdit.GetSelectionToColumn();
	}

	public void SetRichPresence(string token, string? playerGroup, int? groupSize)
	{
	}

	public void SetRichPresenceValue(string key, string? value)
	{
	}

	public void ClearRichPresence()
	{
	}

	public PlatformBranch GetPlatformBranch()
	{
		return PlatformBranch.None;
	}

	public string? GetThreeLetterLanguageCode()
	{
		CultureInfo cultureInfo = new CultureInfo(GetRawLanguage());
		if (LocManager.Languages.Contains(cultureInfo.ThreeLetterISOLanguageName))
		{
			return cultureInfo.ThreeLetterISOLanguageName;
		}
		string text = cultureInfo.Name.ToLowerInvariant();
		text = text.Replace('-', '_');
		if (text.StartsWith("zh"))
		{
			if (text.StartsWith("zh_hans") || text.StartsWith("zh_cn"))
			{
				return "zhs";
			}
			if (text.StartsWith("zh_hant") || text.StartsWith("zh_tw"))
			{
				return "zht";
			}
			return "zhs";
		}
		if (text.StartsWith("pt"))
		{
			if (text.StartsWith("pt_br"))
			{
				return "ptb";
			}
			return "por";
		}
		Log.Error($"CultureInfo {cultureInfo} could not be mapped to a three-letter language code!");
		return null;
	}

	public string GetRawLanguage()
	{
		return OS.GetLocale().Replace('_', '-');
	}

	public SupportedWindowMode GetSupportedWindowMode()
	{
		if (OS.HasFeature("mobile"))
		{
			return SupportedWindowMode.FullscreenOnly;
		}
		return SupportedWindowMode.Any;
	}
}
