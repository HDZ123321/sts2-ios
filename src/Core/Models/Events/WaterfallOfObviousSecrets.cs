using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Models.Events;

public sealed class WaterfallOfObviousSecrets : EventModel
{
	private const string _negotiateCostKey = "NegotiateCostKey";

	private const string _beatUpCurseCountKey = "BeatUpCurseCount";

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DynamicVar("NegotiateCostKey", 70m),
		new DynamicVar("BeatUpCurseCount", 2m)
	});

	private static string EnterWawterfallAsset => ImageHelper.GetImagePath("events/waterfall_of_obvious_secrets_gremlin.png");

	public override IEnumerable<string> GetAssetPaths(IRunState runState)
	{
		List<string> list = new List<string>();
		list.AddRange(base.GetAssetPaths(runState));
		list.Add(EnterWawterfallAsset);
		return new _003C_003Ez__ReadOnlyList<string>(list);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return new global::_003C_003Ez__ReadOnlySingleElementList<EventOption>(new EventOption(this, EnterWaterfall, "WATERFALL_OF_OBVIOUS_SECRETS.pages.INITIAL.options.ENTER"));
	}

	private Task EnterWaterfall()
	{
		EventOption eventOption = new EventOption(this, BeNice, "WATERFALL_OF_OBVIOUS_SECRETS.pages.ENTER.options.BE_NICE", HoverTipFactory.FromCardWithCardHoverTips<DingyDagger>());
		EventOption eventOption2 = new EventOption(this, BeatUp, "WATERFALL_OF_OBVIOUS_SECRETS.pages.ENTER.options.BEAT_UP", HoverTipFactory.FromCardWithCardHoverTips<Guilty>());
		EventOption eventOption3 = ((!((decimal)base.Owner.Gold >= base.DynamicVars["NegotiateCostKey"].BaseValue)) ? new EventOption(this, null, "WATERFALL_OF_OBVIOUS_SECRETS.pages.ENTER.options.NEGOTIATE_LOCKED") : new EventOption(this, Negotiate, "WATERFALL_OF_OBVIOUS_SECRETS.pages.ENTER.options.NEGOTIATE"));
		if (LocalContext.IsMe(base.Owner))
		{
			NEventRoom.Instance.SetPortrait(PreloadManager.Cache.GetTexture2D(EnterWawterfallAsset));
		}
		SetEventState(L10NLookup("WATERFALL_OF_OBVIOUS_SECRETS.pages.ENTER.description"), new global::_003C_003Ez__ReadOnlyArray<EventOption>(new EventOption[3] { eventOption, eventOption2, eventOption3 }));
		return Task.CompletedTask;
	}

	private async Task BeNice()
	{
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(base.Owner.RunState.CreateCard<DingyDagger>(base.Owner), PileType.Deck));
		SetEventFinished(L10NLookup("WATERFALL_OF_OBVIOUS_SECRETS.pages.BE_NICE.description"));
	}

	private async Task BeatUp()
	{
		RelicModel relic = RelicFactory.PullNextRelicFromFront(base.Owner, base.Rng).ToMutable();
		await RelicCmd.Obtain(relic, base.Owner);
		await CardPileCmd.AddCursesToDeck(Enumerable.Repeat(ModelDb.Card<Guilty>(), base.DynamicVars["BeatUpCurseCount"].IntValue), base.Owner);
		SetEventFinished(L10NLookup("WATERFALL_OF_OBVIOUS_SECRETS.pages.BEAT_UP.description"));
	}

	private async Task Negotiate()
	{
		await PlayerCmd.LoseGold(base.DynamicVars["NegotiateCostKey"].BaseValue, base.Owner);
		CardCreationOptions options = new CardCreationOptions(new global::_003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(base.Owner.Character.CardPool), CardCreationSource.Other, CardRarityOddsType.Uniform, (CardModel c) => c.Rarity == CardRarity.Rare).WithFlags(CardCreationFlags.NoUpgradeRoll);
		List<CardModel> list = (from r in CardFactory.CreateForReward(base.Owner, 1, options)
			select r.Card).ToList();
		if (list.Count > 0)
		{
			CardModel card = list[0];
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
		}
		SetEventFinished(L10NLookup("WATERFALL_OF_OBVIOUS_SECRETS.pages.NEGOTIATE.description"));
	}
}
