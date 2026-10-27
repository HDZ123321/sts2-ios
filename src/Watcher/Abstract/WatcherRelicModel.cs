using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Extensions;

namespace Watcher.Code.Abstract;

public abstract class WatcherRelicModel : CustomRelicModel
{
	protected override string BigIconPath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".png").BigRelicImagePath();

	public override string PackedIconPath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".tres").TresRelicImagePath();

	protected override string PackedIconOutlinePath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + "_outline.tres").TresRelicImagePath();

	protected WatcherRelicModel()
		: base(true)
	{
	}
}
