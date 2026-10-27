using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Powers;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Multiplayer;

[Pool(typeof(WatcherCardPool))]
public class FleetingClarity : WatcherCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public FleetingClarity()
		: base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AllAllies)
	{
		WithPower<MultiplayerCardUncommonPower>(1, false);
		WithStanceTip<CalmStance>();
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Exhaust, (UpgradeType)2);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)(object)this).CombatState == null)
		{
			return;
		}
		List<Creature> players = ((CardModel)(object)this).CombatState.PlayerCreatures.Where((Creature c) => c.IsAlive && c != ((CardModel)(object)this).Owner.Creature).ToList();
		await CommonActions.Apply<MultiplayerCardUncommonPower>(ctx, (IEnumerable<Creature>)players, DynamicVarSource.op_Implicit((CardModel)(object)this), true);
		foreach (Player item in players.Select((Creature e) => e.Player))
		{
			if (item != null)
			{
				await StanceCmd.EnterCalm(ctx, item, (CardModel?)(object)this);
			}
		}
	}
}
