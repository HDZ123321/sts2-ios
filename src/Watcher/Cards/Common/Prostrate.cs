using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class Prostrate : WatcherCardModel
{
	public Prostrate()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(4, 0);
		((ConstructedCardModel)this).WithPower<MantraPower>(2, 1);
		WithStanceTip<DivinityStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<MantraPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
