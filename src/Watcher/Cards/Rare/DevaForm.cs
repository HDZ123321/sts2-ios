using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class DevaForm : WatcherCardModel
{
	public DevaForm()
		: base(3, CardType.Power, CardRarity.Rare, TargetType.None)
	{
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Ethereal, (UpgradeType)2);
		WithPower<DevaPower>(1, false);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DevaPower>(ctx, (CardModel)(object)this, false);
	}
}
