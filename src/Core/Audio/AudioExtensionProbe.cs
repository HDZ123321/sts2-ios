using System;
using System.Runtime.InteropServices;
using Godot;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace MegaCrit.Sts2.Core.Audio;

/// <summary>
/// 音频扩展诊断探针（静态工具，由 TouchInputAdapter 在 _Ready 时调用一次）。
///
/// 背景：fmod 的 GDExtension 在 iOS 上从未注册成功（所有 FmodServer 引用都变成
/// "Identifier not declared"），但 GDExtensionManager 的失败路径只走 ERR_PRINT，
/// 输出到 stderr 而不是 godot.log，所以从日志里完全看不到原因。
///
/// 本探针做两件事：
///   1. 查询引擎侧状态（ClassDB / GDExtensionManager / 文件是否存在）
///   2. 绕过引擎，直接用 NativeLibrary.TryLoad 分别 dlopen 两份副本，
///      并打印 errno —— 这是唯一能拿到真实失败原因（比如未签名）的办法
///
/// 只读，不改任何状态。
/// </summary>
public static class AudioExtensionProbe
{
	private const string FmodExtensionPath = "res://addons/fmod/fmod.gdextension";
	private const string SpineExtensionPath = "res://addons/spine/spine_godot_extension.gdextension";

	private static bool _done;

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
	}

	private static void Probe()
	{
		Log("=== AudioExtensionProbe start ===");
		Log("platform: " + OS.GetName() + " / " + Engine.GetArchitectureName());
		Log("version: " + Engine.GetVersionInfo()["string"]);

		Log("has_feature(dotnet)           = " + OS.HasFeature("dotnet"));
		Log("has_feature(mobile)           = " + OS.HasFeature("mobile"));
		Log("has_feature(ios)              = " + OS.HasFeature("ios"));
		Log("has_feature(iOS)              = " + OS.HasFeature("iOS"));
		Log("has_feature(template_release) = " + OS.HasFeature("template_release"));
		Log("has_feature(release)          = " + OS.HasFeature("release"));

		// ClassDB 里能不能看见扩展注册的类？这是判断扩展有没有注册的最直接证据。
		foreach (string cls in new[]
		{
			"FmodServer", "FmodBankLoader", "FmodListener2D", "FmodEventEmitter2D",
			"SpineSprite", "SpineMesh2D", "SpineSkeletonDataResource",
		})
		{
			Log("ClassDB.class_exists(" + cls + ") = " + ClassDB.ClassExists(cls));
		}

		// GDExtensionManager 里已加载的扩展列表。
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

		// 两个扩展各自的加载状态。
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

		// 文件到底在不在包里？相对/绝对两种写法都查，并打印 globalize 结果。
		foreach (string p in new[]
		{
			FmodExtensionPath,
			SpineExtensionPath,
			"res://addons/fmod/libs/ios/fmod.ios.template_release.framework/fmod.ios.template_release",
			"res://addons/fmod/libs/ios/fmod.ios.template_release.framework/Info.plist",
			"res://addons/fmod/libs/ios/fmod.ios.template_debug.framework/fmod.ios.template_debug",
			"res://addons/spine/ios/libspine_godot.ios.template_release.framework/libspine_godot.ios.template_release",
		})
		{
			bool fileExists = Godot.FileAccess.FileExists(p);
			bool dirExists = DirAccess.DirExistsAbsolute(p);
			Log("path " + p + " -> file=" + fileExists + " dir=" + dirExists
				+ " globalized=" + ProjectSettings.GlobalizePath(p));
		}

		// 真正再调一次 load_extension，把返回码打出来。
		// 已成功的返回 AlreadyLoaded，失败的返回 Failed。
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
		// 引擎的失败原因只进 stderr，这里直接看操作系统给的错误码。
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
				// 顺手查一下入口符号在不在，这能区分「库能开但符号没有」和「库根本开不了」
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
				Log("dlopen[" + label + "] = FAILED errno=" + errno);
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
		GD.Print(line);
		try
		{
			MegaCrit.Sts2.Core.Logging.Log.Info(line);
		}
		catch (Exception)
		{
		}
	}
}
