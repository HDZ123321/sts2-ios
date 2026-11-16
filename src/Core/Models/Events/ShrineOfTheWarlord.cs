using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class ShrineOfTheWarlord : EventModel
{
	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[3]
	{
		new StringVar("WeaponTitle", ModelDb.Card<MightyWeapon>().Title),
		new StringVar("ShieldTitle", ModelDb.Card<MightyShield>().Title),
		new StringVar("WingsTitle", ModelDb.Relic<MightyWings>().Title.GetFormattedText())
	});

	public override bool IsAllowed(IRunState runState)
	{
		return !(runState.Act.Ancient is Tanx);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		EventOption[] obj = new EventOption[3]
		{
			new EventOption(this, TakeWeapon, "SHRINE_OF_THE_WARLORD.pages.INITIAL.options.TAKE_WEAPON", HoverTipFactory.FromCardWithCardHoverTips<MightyWeapon>()),
			new EventOption(this, TakeShield, "SHRINE_OF_THE_WARLORD.pages.INITIAL.options.TAKE_SHIELD", HoverTipFactory.FromCardWithCardHoverTips<MightyShield>()),
			null
		};
		Func<Task> onChosen = TakeWings;
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(HoverTipFactory.FromRelic<MightyWings>());
		list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<Clumsy>());
		obj[2] = new EventOption(this, onChosen, "SHRINE_OF_THE_WARLORD.pages.INITIAL.options.TAKE_WINGS", list.ToArray());
		return new global::_003C_003Ez__ReadOnlyArray<EventOption>(obj);
	}

	private async Task TakeWeapon()
	{
		CardModel card = base.Owner.RunState.CreateCard<MightyWeapon>(base.Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
		SetEventFinished(L10NLookup("SHRINE_OF_THE_WARLORD.pages.TAKE_WEAPON.description"));
	}

	private async Task TakeShield()
	{
		CardModel card = base.Owner.RunState.CreateCard<MightyShield>(base.Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
		SetEventFinished(L10NLookup("SHRINE_OF_THE_WARLORD.pages.TAKE_SHIELD.description"));
	}

	private async Task TakeWings()
	{
		await CardPileCmd.AddCurseToDeck<Clumsy>(base.Owner);
		await RelicCmd.Obtain<MightyWings>(base.Owner);
		SetEventFinished(L10NLookup("SHRINE_OF_THE_WARLORD.pages.TAKE_WINGS.description"));
	}
}
