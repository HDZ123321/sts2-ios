using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class MythicFormPower : PowerModel
{
	private const string _upgradedKey = "Upgraded";

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("Upgraded", 0m));

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(StaticHoverTip.Tarot));

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player != base.Owner.Player)
		{
			return;
		}
		Flash();
		int upgradedCount = base.DynamicVars["Upgraded"].IntValue;
		for (int i = 0; i < base.Amount; i++)
		{
			CardModel cardModel = player.RunState.Rng.CombatCardSelection.NextItem(TarotCards.All.Where((CardModel c) => c.IsAllowedInRun(player.RunState)));
			if (cardModel != null)
			{
				CardModel card = combatState.CreateCard(cardModel, player);
				if (i < upgradedCount)
				{
					CardCmd.Upgrade(card);
				}
				await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
			}
		}
	}

	public void IncrementUpgraded()
	{
		AssertMutable();
		base.DynamicVars["Upgraded"].BaseValue++;
	}
}
