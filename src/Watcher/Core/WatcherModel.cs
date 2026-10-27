using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Watcher.Code.Events;
using Watcher.Code.Nodes;
using Watcher.Code.Stances;

namespace Watcher.Code.Core;

public class WatcherModel : CustomSingletonModel
{
	private static readonly SpireField<Player, WatcherStanceModel> ActiveStance = new SpireField<Player, WatcherStanceModel>((Func<WatcherStanceModel>)WatcherModelDb.WatcherStance<NoStance>);

	public override bool ShouldReceiveCombatHooks => true;

	public WatcherModel()
		: base((HookType)1)
	{
	}

	public static WatcherStanceModel GetStanceModel(Player player)
	{
		return ActiveStance[player] ?? WatcherModelDb.WatcherStance<NoStance>();
	}

	public static bool IsInStance<T>(Player player) where T : WatcherStanceModel
	{
		return ActiveStance[player] is T;
	}

	public static async Task SetStance<T>(PlayerChoiceContext ctx, Player player, CardModel? source) where T : WatcherStanceModel
	{
		await SetStance(ctx, player, WatcherModelDb.WatcherStance<T>(), source);
	}

	private static async Task SetStance(PlayerChoiceContext ctx, Player player, WatcherStanceModel newCanonical, CardModel? source)
	{
		WatcherStanceModel current = ActiveStance[player];
		if (!(current?.GetType() == newCanonical.GetType()))
		{
			if (current != null)
			{
				await current.OnExitStance(ctx, player, source);
			}
			WatcherStanceModel mutable = newCanonical.ToMutable(player);
			ActiveStance[player] = mutable;
			await mutable.OnEnterStance(ctx, player, source);
			if ((NCombatRoom.Instance?.GetCreatureNode(player.Creature))?.Visuals is WatcherNCreatureVisuals watcherNCreatureVisuals)
			{
				string eyeStance = ((mutable is WrathStance) ? "Wrath" : ((mutable is CalmStance) ? "Calm" : ((!(mutable is DivinityStance)) ? "None" : "Divinity")));
				watcherNCreatureVisuals.SetEyeStance(eyeStance);
			}
			await WatcherHook.OnStanceChange(ctx, player, current, ActiveStance[player]);
		}
	}

	public override Task BeforeCombatStart()
	{
		CombatState combatState = CombatManager.Instance.DebugOnlyGetState();
		if (combatState == null)
		{
			return Task.CompletedTask;
		}
		foreach (Player player in combatState.Players)
		{
			ActiveStance[player] = WatcherModelDb.WatcherStance<NoStance>();
		}
		return Task.CompletedTask;
	}
}
