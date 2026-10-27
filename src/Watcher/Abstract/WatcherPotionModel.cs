using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Extensions;

namespace Watcher.Code.Abstract;

public abstract class WatcherPotionModel : CustomPotionModel
{
	public override string CustomPackedImagePath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".tres").PackedPotionImagePath();

	public override string CustomPackedOutlinePath => (StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + "_outline.tres").PackedPotionImagePath();
}
