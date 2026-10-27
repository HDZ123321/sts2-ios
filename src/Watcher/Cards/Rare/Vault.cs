using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Vault : WatcherCardModel
{
	public Vault()
		: base(3, CardType.Skill, CardRarity.Rare, TargetType.None)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		WithPower<VaultPower>(1, false);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<VaultPower>(ctx, (CardModel)(object)this, false);
		PlayerCmd.EndTurn(((CardModel)(object)this).Owner, canBackOut: false);
	}
}
