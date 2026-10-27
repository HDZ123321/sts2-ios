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
public sealed class Smite : WatcherCardModel
{
	public Smite()
		: base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(12, 4);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[2]
		{
			CardKeyword.Retain,
			CardKeyword.Exhaust
		});
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(choiceContext);
	}
}
