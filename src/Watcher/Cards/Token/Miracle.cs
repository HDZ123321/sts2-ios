using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Watcher.Code.Abstract;

namespace Watcher.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Miracle : WatcherCardModel
{
	public Miracle()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
		((ConstructedCardModel)this).WithEnergyTip();
		((ConstructedCardModel)this).WithVars(new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<EnergyVar>(new EnergyVar(1), 1m) });
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[2]
		{
			CardKeyword.Retain,
			CardKeyword.Exhaust
		});
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PlayerCmd.GainEnergy(((CardModel)(object)this).DynamicVars.Energy.IntValue, ((CardModel)(object)this).Owner);
	}
}
