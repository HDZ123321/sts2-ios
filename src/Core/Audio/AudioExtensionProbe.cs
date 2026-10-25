using System;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace MegaCrit.Sts2.Core.Audio;

/// <summary>
/// 音频扩展诊断探针（静态工具，由 NGame._EnterTree 与 TouchInputAdapter._Ready 各尝试调用一次，
/// _done 保证只跑一遍）。
///
/// 背景：fmod 的 GDExtension 在 iOS 上从未注册成功（所有 FmodServer 引用都变成
/// "Identifier not declared"），但 GDExtensionManager 的失败路径只走 ERR_PRINT，
/// 输出到 stderr 而不是 godot.log，所以从日志里完全看不到原因。
///
/// 本探针做三件事：
///   1. 查询引擎侧状态（ClassDB / GDExtensionManager / 文件是否存在）
///   2. 绕过引擎，直接用 NativeLibrary.TryLoad 分别 dlopen 两份副本，
///      并打印 errno —— 这是唯一能拿到真实失败原因（比如未签名）的办法
///   3. 同时把结果写进 user://audioprobe.txt，日志通道失效时还能从「文件」App 取
///
/// 只读，不改任何状态（除了显式 LoadExtension 那一步，那一步本来也应该发生）。
/// </summary>
public static class AudioExtensionProbe
{
	/// <summary>构建戳。带数字与连字符，保证在二进制里是唯一子串，可用于核对发布包是否含本次改动。</summary>
	public const string BuildStamp = "probe-v13-20261025T1730";

	private const string FmodExtensionPath = "res://addons/fmod/fmod.gdextension";
	private const string SpineExtensionPath = "res://addons/spine/spine_godot_extension.gdextension";
	private const string DumpPath = "user://audioprobe.txt";

	private static bool _done;
	private static readonly StringBuilder _dump = new StringBuilder(8192);

	public static void RunOnce()
	{
		if (_done)
		{
			return;
		}
		_done = true;
		try
		{
			Probe();
		}
		catch (Exception ex)
		{
			Log("probe threw: " + ex);
		}
		Flush();
	}

	private static void Probe()
	{
		Log("=== AudioExtensionProbe start ===");
		Log("stamp: " + BuildStamp);
		Log("platform: " + OS.GetName() + " / " + Engine.GetArchitectureName());
		Log("version: " + Engine.GetVersionInfo()["string"]);

		Log("has_feature(dotnet)           = " + OS.HasFeature("dotnet"));
		Log("has_feature(mobile)           = " + OS.HasFeature("mobile"));
		Log("has_feature(ios)              = " + OS.HasFeature("ios"));
		Log("has_feature(iOS)              = " + OS.HasFeature("iOS"));
		Log("has_feature(template_release) = " + OS.HasFeature("template_release"));
		Log("has_feature(release)          = " + OS.HasFeature("release"));
		Log("has_feature(arm64)            = " + OS.HasFeature("arm64"));

		foreach (string cls in new[]
		{
			"FmodServer", "FmodBankLoader", "FmodListener2D", "FmodEventEmitter2D",
			"SpineSprite", "SpineMesh2D", "SpineSkeletonDataResource",
		})
		{
			Log("ClassDB.class_exists(" + cls + ") = " + ClassDB.ClassExists(cls));
		}

		try
		{
			string[] loaded = GDExtensionManager.GetLoadedExtensions();
			Log("loaded extensions (" + loaded.Length + "):");
			foreach (string e in loaded)
			{
				Log("  " + e);
			}
		}
		catch (Exception ex)
		{
			Log("GetLoadedExtensions failed: " + ex.GetType().Name + ": " + ex.Message);
		}

		foreach (string path in new[] { FmodExtensionPath, SpineExtensionPath })
		{
			try
			{
				Log("is_extension_loaded(" + path + ") = " + GDExtensionManager.IsExtensionLoaded(path));
			}
			catch (Exception ex)
			{
				Log("IsExtensionLoaded(" + path + ") failed: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		foreach (string p in new[]
		{
			FmodExtensionPath,
			SpineExtensionPath,
			"res://addons/fmod/libs/ios/fmod.ios.template_release.framework/fmod.ios.template_release",
			"res://addons/fmod/libs/ios/fmod.ios.template_release.framework/Info.plist",
			"res://addons/spine/ios/libspine_godot.ios.template_release.framework/libspine_godot.ios.template_release",
		})
		{
			bool fileExists = Godot.FileAccess.FileExists(p);
			bool dirExists = DirAccess.DirExistsAbsolute(p);
			Log("path " + p + " -> file=" + fileExists + " dir=" + dirExists
				+ " globalized=" + ProjectSettings.GlobalizePath(p));
		}

		foreach (string path in new[] { FmodExtensionPath, SpineExtensionPath })
		{
			try
			{
				GDExtensionManager.LoadStatus st = GDExtensionManager.LoadExtension(path);
				Log("LoadExtension(" + path + ") = " + st);
			}
			catch (Exception ex)
			{
				Log("LoadExtension(" + path + ") threw: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		foreach (string path in new[] { FmodExtensionPath, SpineExtensionPath })
		{
			Log("after: is_extension_loaded(" + path + ") = " + GDExtensionManager.IsExtensionLoaded(path));
		}
		foreach (string cls in new[] { "FmodServer", "FmodBankLoader", "SpineSprite" })
		{
			Log("after: ClassDB.class_exists(" + cls + ") = " + ClassDB.ClassExists(cls));
		}

		// ===== 关键部分：绕过引擎，自己 dlopen 两份副本，拿 errno =====
		string exePath = OS.GetExecutablePath();
		Log("executable path: " + exePath);
		Log("user data dir  : " + OS.GetUserDataDir());

		string exeDir = IOPath.GetDirectoryName(exePath);
		if (!string.IsNullOrEmpty(exeDir))
		{
			TryLoad("Frameworks/fmod release",
				IOPath.Combine(exeDir, "Frameworks/fmod.ios.template_release.framework/fmod.ios.template_release"));
			TryLoad("Frameworks/spine release",
				IOPath.Combine(exeDir, "Frameworks/libspine_godot.ios.template_release.framework/libspine_godot.ios.template_release"));
			TryLoad("dylibs/fmod release",
				IOPath.Combine(exeDir, "dylibs/addons/fmod/libs/ios/fmod.ios.template_release.framework/fmod.ios.template_release"));
			TryLoad("dylibs/spine release",
				IOPath.Combine(exeDir, "dylibs/addons/spine/ios/libspine_godot.ios.template_release.framework/libspine_godot.ios.template_release"));
			TryLoad("Frameworks/sts2.dylib",
				IOPath.Combine(exeDir, "Frameworks/sts2.dylib"));
		}

		Log("=== AudioExtensionProbe end ===");
	}

	private static void TryLoad(string label, string path)
	{
		bool exists = IOFile.Exists(path);
		Log("dlopen[" + label + "] exists=" + exists + " path=" + path);
		if (!exists)
		{
			return;
		}
		try
		{
			if (NativeLibrary.TryLoad(path, out IntPtr handle))
			{
				Log("dlopen[" + label + "] = OK handle=0x" + handle.ToString("x"));
				foreach (string sym in new[] { "fmod_library_init", "spine_godot_library_init" })
				{
					if (NativeLibrary.TryGetExport(handle, sym, out IntPtr addr))
					{
						Log("  export " + sym + " = 0x" + addr.ToString("x"));
					}
				}
				NativeLibrary.Free(handle);
			}
			else
			{
				int errno = Marshal.GetLastPInvokeError();
				Log("dlopen[" + label + "] = FAILED errno=" + errno
					+ " (" + Marshal.GetPInvokeErrorMessage(errno) + ")");
			}
		}
		catch (Exception ex)
		{
			Log("dlopen[" + label + "] threw: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void Log(string msg)
	{
		string line = "[AudioProbe] " + msg;
		_dump.Append(line).Append('\n');
		GD.Print(line);
		try
		{
			MegaCrit.Sts2.Core.Logging.Log.Info(line);
		}
		catch (Exception)
		{
		}
	}

	private static void Flush()
	{
		try
		{
			using Godot.FileAccess f = Godot.FileAccess.Open(DumpPath, Godot.FileAccess.ModeFlags.Write);
			if (f != null)
			{
				f.StoreString(_dump.ToString());
			}
		}
		catch (Exception)
		{
		}
	}
}
