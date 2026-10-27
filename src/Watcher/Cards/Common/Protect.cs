using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class Protect : WatcherCardModel
{
	public Protect()
		: base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(12, 4);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Retain });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
