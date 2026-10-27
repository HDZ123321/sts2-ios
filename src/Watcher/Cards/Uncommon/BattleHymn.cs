using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class BattleHymn : WatcherCardModel
{
	public BattleHymn()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
	{
		WithPower<BattleHymnPower>(1, false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Smite)));
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Innate, (UpgradeType)1);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<BattleHymnPower>(ctx, (CardModel)(object)this, false);
	}
}
