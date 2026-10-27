using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Watcher.Code.Extensions;
using Watcher.Code.Relics;

namespace Watcher.Code.Powers;

public class DualityPower : CustomTemporaryPowerModelWrapper<Duality, DexterityPower>
{
	public override string CustomPackedIconPath => (StringExtensions.RemovePrefix(((AbstractModel)(object)this).Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

	public override string CustomBigIconPath => ((CustomPowerModel)this).CustomPackedIconPath;
}
