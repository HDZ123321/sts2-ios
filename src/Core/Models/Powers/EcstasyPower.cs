using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class EcstasyPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>(new IHoverTip[2]
	{
		HoverTipFactory.FromCard<Sadness>(),
		HoverTipFactory.FromCard<Joy>()
	});

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player)
		{
			return;
		}
		List<Sadness> list = PileType.Hand.GetPile(base.Owner.Player).Cards.OfType<Sadness>().ToList();
		if (list.Count == 0)
		{
			return;
		}
		list.StableShuffle(base.Owner.Player.RunState.Rng.CombatCardSelection);
		List<Sadness> list2 = list.Take(base.Amount).ToList();
		Flash();
		List<CardTransformation> list3 = new List<CardTransformation>();
		foreach (Sadness item in list2)
		{
			CardModel replacement = base.CombatState.CreateCard<Joy>(base.Owner.Player);
			list3.Add(new CardTransformation(item, replacement));
		}
		await CardCmd.Transform(list3, null);
	}
}
