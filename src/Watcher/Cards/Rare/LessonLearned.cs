using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class LessonLearned : WatcherCardModel
{
	public override bool CanBeGeneratedInCombat => false;

	public LessonLearned()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(10, 3);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(StaticHoverTip.Fatal));
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Target == null)
		{
			return;
		}
		bool shouldTriggerFatal = cardPlay.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
		AttackCommand attackCommand = await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
		if (!shouldTriggerFatal || !attackCommand.Results.Any((List<DamageResult> r) => r.Any(delegate(DamageResult g)
		{
			if (g != null && g.WasTargetKilled)
			{
				Creature receiver = g.Receiver;
				if (receiver != null)
				{
					return !receiver.IsSecondaryEnemy;
				}
			}
			return false;
		})))
		{
			return;
		}
		List<CardModel> upgradableCards = PileType.Deck.GetPile(((CardModel)(object)this).Owner).Cards.Where((CardModel c) => c.IsUpgradable).ToList();
		if (upgradableCards.Count <= 0)
		{
			return;
		}
		await Cmd.Wait(0.5f);
		CardModel cardModel = ((CardModel)(object)this).Owner.RunState.Rng.Niche.NextItem(upgradableCards);
		if (cardModel != null)
		{
			((CardModel)(object)this).Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(((CardModel)(object)this).Owner.NetId).UpgradedCards.Add(cardModel.Id);
			cardModel.UpgradeInternal();
			cardModel.FinalizeUpgradeInternal();
			if (LocalContext.IsMe(((CardModel)(object)this).Owner))
			{
				NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(NCardSmithVfx.Create(new global::_003C_003Ez__ReadOnlySingleElementList<CardModel>(cardModel)));
			}
		}
	}
}
