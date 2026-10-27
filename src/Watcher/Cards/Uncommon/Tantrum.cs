using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Tantrum : WatcherCardModel
{
	public Tantrum()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(3, 0);
		((ConstructedCardModel)this).WithVars(new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<RepeatVar>(new RepeatVar(3), 1m) });
		WithStanceTip<WrathStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(((CardModel)(object)this).DynamicVars.Repeat.IntValue).Execute(ctx);
		await StanceCmd.EnterWrath(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
		await Cmd.Wait(0.25f);
		await CardPileCmd.Add((CardModel)(object)this, PileType.Draw, CardPilePosition.Random);
	}
}
