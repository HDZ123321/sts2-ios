using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Extensions;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class Halt : WatcherCardModel
{
	public Halt()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(3, 1);
		((ConstructedCardModel)this).WithVar((DynamicVar)DynamicVarExtensions.WithUpgrade<BlockVar>(new BlockVar("WrathBlock", 9m, ValueProp.Move), 5m));
		WithStanceTip<WrathStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (((CardModel)(object)this).Owner.IsInWatcherStance<WrathStance>())
		{
			await CreatureCmd.GainBlock(((CardModel)(object)this).Owner.Creature, ((CardModel)(object)this).DynamicVars["WrathBlock"].BaseValue, ValueProp.Move, cardPlay);
		}
	}
}
