using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Extensions;

namespace Watcher.Code.Abstract;

public abstract class WatcherPowerModel : CustomPowerModel
{
	public sealed override string CustomPackedIconPath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

	public sealed override string CustomBigIconPath => ((CustomPowerModel)this).CustomPackedIconPath;
}
