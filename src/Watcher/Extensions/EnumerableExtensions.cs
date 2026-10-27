using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Watcher.Code.Extensions;

public static class EnumerableExtensions
{
	public static IEnumerable<CardModel> FilterForPlayerCount(this IEnumerable<CardModel> options, IRunState runState)
	{
		if (runState.Players.Count <= 1)
		{
			return options.Where((CardModel c) => c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly);
		}
		return options.Where((CardModel c) => c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly);
	}
}
