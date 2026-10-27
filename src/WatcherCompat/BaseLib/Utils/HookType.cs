namespace BaseLib.Utils;

/// <summary>
/// Which dispatch chain a singleton model subscribes to.
/// </summary>
public enum HookType
{
	Run = 0,
	Combat = 1,
	RunAndCombat = 2
}
