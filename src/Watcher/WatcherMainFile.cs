using System.Reflection;
using BaseLib.Utils;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using Watcher.Code.Events;
using Watcher.Code.Patches;

namespace Watcher.Code;

[ModInitializer("Initialize")]
public static class WatcherMainFile
{
	public const string ModId = "Watcher";

	public static Logger Logger { get; } = new Logger("Watcher", LogType.Generic);

	public static void Initialize()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		WatcherSubscriber.Subscribe();
		ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
		ModPatcher modPatcher = ModPatcher.Create("Watcher", Logger);
		modPatcher.Add(typeof(StanceModifyDamageMultiplicativePatch)).Add(typeof(PaelsEyeSourceTrackingPatches)).Add(typeof(ColorfulPhilosophersPatch));
		modPatcher.PatchAll();
		ModCredits.Register("Watcher", (Section[])(object)new Section[3]
		{
			new Section("Dev", (Layout)1, (Section[])null),
			new Section("Art", (Layout)2, (Section[])null),
			new Section("Loc", (Layout)2, (Section[])null)
		});
	}
}
