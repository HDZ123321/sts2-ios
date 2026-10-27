using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Hooks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Weave : WatcherCardModel, IAfterScryed
{
	public Weave()
		: base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(BaseLibTip.Scry));
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(choiceContext);
		}
	}

	public async Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded)
	{
		if (player == ((CardModel)(object)this).Owner && PileType.Discard.GetPile(player).Cards.Contains((CardModel)(object)this))
		{
			await CardPileCmd.Add((CardModel)(object)this, PileType.Hand);
		}
	}
}
