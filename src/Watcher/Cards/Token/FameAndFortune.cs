using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Watcher.Code.Abstract;

namespace Watcher.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class FameAndFortune : WishableWatcherCard
{
	public FameAndFortune()
		: base(-1, CardType.Skill, CardRarity.Token, TargetType.None)
	{
		((ConstructedCardModel)this).WithVars(new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<GoldVar>(new GoldVar(25), 5m) });
	}

	public override async Task OnWish(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		VfxCmd.PlayOnCreature(((CardModel)(object)this).Owner.Creature, "vfx/vfx_coin_explosion_regular");
		await PlayerCmd.GainGold(((CardModel)(object)this).DynamicVars.Gold.IntValue, ((CardModel)(object)this).Owner);
	}
}
