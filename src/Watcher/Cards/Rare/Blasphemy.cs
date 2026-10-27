using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Powers;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Blasphemy : WatcherCardModel
{
	public Blasphemy()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
		((ConstructedCardModel)this).WithPower<BlasphemerPower>(1, 0);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		WithStanceTip<DivinityStance>();
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Retain, (UpgradeType)1);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<BlasphemerPower>(ctx, (CardModel)(object)this, false);
		await StanceCmd.EnterDivinity(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
