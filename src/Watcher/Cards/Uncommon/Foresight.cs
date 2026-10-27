using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Foresight : WatcherCardModel
{
	public Foresight()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
	{
		WithPower<ForesightPower>(3, 1, false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(BaseLibTip.Scry));
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<ForesightPower>(ctx, (CardModel)(object)this, false);
	}
}
