using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Hooks;

/// <summary>Fired on every model in combat after a scry resolves.</summary>
public interface IAfterScryed
{
	Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded);
}

/// <summary>Lets a relic or power widen a scry before the cards are revealed.</summary>
public interface IModifyScryAmount
{
	int ModifyScryAmount(Player player, int amount);
}

/// <summary>Optional follow-up for <see cref="T:BaseLib.Hooks.IModifyScryAmount"/> holders.</summary>
public interface IAfterModifyingScryAmount
{
	Task AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount);
}
