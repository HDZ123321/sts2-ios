using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Watcher.Code.Abstract;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class LiveForever : WishableWatcherCard
{
	public LiveForever()
		: base(-1, CardType.Power, CardRarity.Token, TargetType.None)
	{
		((ConstructedCardModel)this).WithPower<PlatedArmorPower>(6, 2);
	}

	public override async Task OnWish(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<PlatedArmorPower>(ctx, (CardModel)(object)this, false);
	}
}
