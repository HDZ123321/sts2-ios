using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Marker power that stands in for a real power for one turn.  The card grants
/// the wrapper, the wrapper forwards its amount to <typeparamref name="TPower"/>,
/// and the wrapper itself is dropped when the side's turn ends.
/// </summary>
public abstract class CustomTemporaryPowerModelWrapper<TCard, TPower> : CustomPowerModel
	where TCard : AbstractModel
	where TPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		if (Amount <= 0)
		{
			return;
		}
		TPower canonical = ModelDb.GetById<TPower>(ModelDb.GetId<TPower>());
		PowerModel instance = canonical.ToMutable(Amount);
		Owner.ApplyPowerInternal(instance);
		await Task.CompletedTask;
	}
}
