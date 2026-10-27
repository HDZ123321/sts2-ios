using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Ancient;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Basic;

[Pool(typeof(WatcherCardPool))]
public sealed class Eruption : WatcherCardModel, ITranscendenceCard
{
	public Eruption()
		: base(2, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(9, 0);
		WithStanceTip<WrathStance>();
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<AncientCard2>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
		await StanceCmd.EnterWrath(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
