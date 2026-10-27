using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Collect : WatcherCardModel
{
	protected override bool HasEnergyCostX => true;

	public Collect()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected O, but got Unknown
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithTip(new TooltipSource((Func<CardModel, IHoverTip>)delegate
		{
			CardModel cardModel = ((CardModel)(object)ModelDb.GetById<Miracle>(((AbstractModel)(object)ModelDb.Card<Miracle>()).Id)).ToMutable();
			cardModel.UpgradeInternal();
			return HoverTipFactory.FromCard(cardModel);
		}));
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = ((CardModel)(object)this).ResolveEnergyXValue();
		if (((CardModel)(object)this).IsUpgraded)
		{
			num++;
		}
		await CommonActions.ApplySelf<CollectPower>(ctx, (CardModel)(object)this, (decimal)num, false);
	}
}
