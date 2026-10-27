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
using Watcher.Code.Powers;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Pray : WatcherCardModel
{
	public Pray()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithPower<MantraPower>(3, 1);
		WithStanceTip<DivinityStance>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Insight)));
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<MantraPower>(ctx, (CardModel)(object)this, false);
		await WatcherCmd.GiveCard<Insight>(((CardModel)(object)this).Owner, PileType.Draw, CardPilePosition.Random, 0.6f, CardPreviewStyle.HorizontalLayout, false, false);
	}
}
