using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Watcher.Code.Compatibility;

public record struct CardLocationCompatiblity(Player Player, PileType PileType, CardPilePosition Position);
