using Godot;
using MegaCrit.Sts2.Core.Assets;

namespace Watcher.Code.Stances.Vfx;

public static class StanceVfx
{
	public const float VfxScale = 0.9f;

	public static bool TryGetTexture(string path, ref Texture2D cached)
	{
		if (!GodotObject.IsInstanceValid(cached))
		{
			cached = PreloadManager.Cache.GetAsset<Texture2D>(path);
		}
		return GodotObject.IsInstanceValid(cached);
	}
}
