using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Establishment : WatcherCardModel
{
	public Establishment()
		: base(1, CardType.Power, CardRarity.Rare, TargetType.None)
	{
		WithPower<EstablishmentPower>(1, false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(CardKeyword.Retain));
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Innate, (UpgradeType)1);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<EstablishmentPower>(ctx, (CardModel)(object)this, false);
	}
}
