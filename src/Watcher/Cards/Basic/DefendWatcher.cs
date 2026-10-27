using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Basic;

[Pool(typeof(WatcherCardPool))]
public sealed class DefendWatcher : WatcherCardModel
{
	public DefendWatcher()
		: base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(5, 3);
		((ConstructedCardModel)this).WithTags(new CardTag[1] { CardTag.Defend });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
