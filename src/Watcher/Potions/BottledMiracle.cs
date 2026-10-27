using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Commands;

namespace Watcher.Code.Potions;

[Pool(typeof(WatcherPotionPool))]
public class BottledMiracle : WatcherPotionModel
{
	private const int BaseAmount = 2;

	public override PotionRarity Rarity => PotionRarity.Common;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AnyPlayer;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new CardsVar(2));

	protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
	{
		if (target?.Player != null)
		{
			int intValue = ((PotionModel)(object)this).DynamicVars.Cards.IntValue;
			await WatcherCmd.GiveCards<Miracle>(target.Player, intValue, PileType.Hand, CardPilePosition.Bottom, 0.6f, CardPreviewStyle.HorizontalLayout, false, false);
		}
	}
}
