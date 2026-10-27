using System.Reflection;
using MegaCrit.Sts2.Core.Saves;

namespace Watcher.Code.Compatibility;

public static class SaveManagerCompat
{
	private static readonly PropertyInfo? IsPrefsLoadedProperty = typeof(SaveManager).GetProperty("IsPrefsLoaded", BindingFlags.Instance | BindingFlags.Public);

	public static bool ArePrefsLoaded(this SaveManager saveManager)
	{
		if (!(IsPrefsLoadedProperty != null))
		{
			return saveManager.PrefsSave != null;
		}
		return (bool)IsPrefsLoadedProperty.GetValue(saveManager);
	}
}
