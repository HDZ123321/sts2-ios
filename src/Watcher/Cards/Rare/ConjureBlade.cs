using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Commands;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class ConjureBlade : WatcherCardModel
{
	protected override bool HasEnergyCostX => true;

	public ConjureBlade()
		: base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Expunger)));
	}

	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		int x = ((CardModel)(object)this).ResolveEnergyXValue();
		if (((CardModel)(object)this).IsUpgraded)
		{
			x++;
		}
		(await WatcherCmd.GiveCard<Expunger>(((CardModel)(object)this).Owner, PileType.Draw, CardPilePosition.Random, 0.6f, CardPreviewStyle.HorizontalLayout, false, false))?.DynamicVars.Repeat.UpgradeValueBy(x + 1);
	}
}
