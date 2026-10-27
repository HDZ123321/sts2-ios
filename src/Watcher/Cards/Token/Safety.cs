using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Watcher.Code.Abstract;

namespace Watcher.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Safety : WatcherCardModel
{
	public Safety()
		: base(1, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(12, 4);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[2]
		{
			CardKeyword.Retain,
			CardKeyword.Exhaust
		});
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
