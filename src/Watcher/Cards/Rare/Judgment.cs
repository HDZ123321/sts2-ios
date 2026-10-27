using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Judgment : WatcherCardModel
{
	public Judgment()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithVar("DamageThreshold", 30, 10);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			Creature target = cardPlay.Target;
			if (target.CurrentHp <= ((CardModel)(object)this).DynamicVars["DamageThreshold"].IntValue)
			{
				target.SetCurrentHpInternal(0m);
				await CreatureCmd.Kill(target);
			}
		}
	}
}
