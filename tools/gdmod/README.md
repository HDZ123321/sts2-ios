# GDScript mods

This document describes the mod format this build supports and how to write a mod.

## What a mod can and cannot be

The game ships as a NativeAOT build. There is no JIT, no `Reflection.Emit`, and no way to load a
managed assembly at runtime: `AssemblyLoadContext.LoadFromAssemblyPath` ends in a
`PlatformNotSupportedException` on this runtime, and Harmony's `PatchAll` needs `Reflection.Emit`.
A mod that ships a `.dll` therefore cannot work here, and neither can a mod that expects to add new
C# model types, because `ModelDb.GetId` derives an id from the CLR type and no new CLR types can be
created at runtime.

What does work is a **GDScript mod**. The engine in this build includes the GDScript module (it is
already used by the fmod addon and several `res://src/gdscript/*.gdc` proxies), and once a PCK is
mounted its contents load through `ResourceLoader` exactly like built-in resources. A `.gd` file
stored in a PCK is compiled by the engine on load, so a modder does not need the Godot editor and
does not need to ship bytecode.

A mod is therefore:

```
<mods dir>/<id>/
    <id>.json      the manifest
    <id>.pck       a resource pack holding at least one GDScript file
```

`<mods dir>` is `user://mods` on iOS and Android, and both `<install dir>/mods` and `user://mods`
on desktop. On iOS `user://` resolves to the app's Documents directory, which the Info.plist already
exposes to the Files app, so a mod can be dropped in from there.

## The manifest

```json
{
  "id": "example_gd",
  "name": "Example GD Mod",
  "author": "tester",
  "description": "what it does",
  "version": "1.0.0",
  "has_pck": true,
  "has_dll": false,
  "affects_gameplay": true,
  "gd_entry": "res://example_gd/main.gd"
}
```

`id` must match the directory name and the file names. `has_dll` must be `false`. `gd_entry` is the
script the host instantiates; when it is absent the host probes `res://<id>/main.gdc` and then
`res://<id>/main.gd`. `min_game_version` is optional and is only used to warn on a mismatch.

## Writing the script

The entry point is a `Node`. The host instantiates it once and parents it under a node of its own;
the instance lives for the whole session.

```gdscript
extends Node

var Sts2

func _ready() -> void:
	Sts2 = Engine.get_singleton("Sts2")
	if Sts2 == null:
		push_error("the Sts2 singleton is not registered")
```

`Sts2` is a singleton object with the methods listed below. Handles are opaque strings: pass them
back to the API, never parse them. `""` means "the current one" wherever a handle is accepted.

### Lifecycle callbacks

The host calls these on every attached mod. Declare the parameter even if you do not use it —
Godot drops a call whose argument count exceeds the method's, so a method declared without the
parameter never runs.

| method | argument |
| --- | --- |
| `on_run_started(run)` | run handle |
| `on_act_entered()` | — |
| `on_room_entered()` | — |
| `on_combat_started(combat)` | combat handle |
| `on_combat_ended(room)` | room handle |
| `on_combat_won(room)` | room handle |
| `on_turn_started(combat)` | combat handle |
| `on_turn_ended(combat)` | combat handle |

### Hook callbacks

Hooks are answered per mod, in mod id order. An additive hook adds its return value to the running
total, so returning `null` (the default when the method is absent) is the same as returning 0. A
multiplicative hook multiplies, and absent means 1, so return `1.0` if you have nothing to say. A
predicate hook's return value overrides the game's answer, so return `null` to abstain.

Additive:

```
modify_damage_additive(target, amount, props, dealer, card_source)
modify_damage_multiplicative(target, amount, props, dealer, card_source)
modify_max_energy(player, amount)
modify_energy_gain(player, amount)
modify_hand_draw(player, count)
modify_x_value(card, value)
modify_gold_gained(player, amount)
modify_merchant_price(player, entry, cost)
modify_rest_site_heal_amount(creature, amount)
modify_orb_value(orb, value)
modify_power_amount_given_additive(power, giver, amount, target, card_source)
modify_power_amount_given_multiplicative(power, giver, amount, target, card_source)
modify_summon_amount(player, amount)
```

Predicates:

```
should_die(creature)
should_draw(player, from_hand_draw)
should_play(card, auto_play_type)
should_add_to_deck(card)
should_flush(player)
should_gain_stars(amount, player)
should_allow_hitting(creature)
should_allow_targeting(creature)
should_clear_block(creature)
should_power_be_removed_on_death(power)
should_procure_potion(potion, player)
should_force_potion_reward(player, room_type)
should_take_extra_turn(player)
should_stop_combat_from_ending()
```

Events:

```
before_card_played(play)
after_card_played(play)
after_card_played_late(play)
after_card_drawn(card, from_hand_draw)
after_card_discarded(card)
after_card_exhausted(card, caused_by_ethereal)
after_damage_received(target, result, props, dealer, card_source)
after_damage_received_late(target, result, props, dealer, card_source)
after_block_gained(creature, amount, props, card_source)
after_block_cleared(creature)
after_current_hp_changed(creature, delta)
after_death(creature, was_removal_prevented, death_anim_length)
before_combat_start()
after_combat_end(room)
after_combat_victory(room)
after_act_entered()
after_room_entered(room)
after_gold_gained(player)
after_potion_used(potion, target)
after_energy_reset(player)
after_energy_spent(card, amount)
after_player_turn_start(player)
```

The `choice_context` argument some hooks carry in C# is dropped on purpose: GDScript cannot hold one.
`result` in the damage hooks is a dictionary with `receiver`, `blocked`, `unblocked`, `overkill`,
`total` and `props`. `play` in the card hooks is a dictionary with `card`, `target`,
`result_pile`, `auto_play`, `play_index` and `play_count`.

## Reading state

```
get_run()  get_player()  get_me()  get_combat()  get_player_combat()  is_in_combat()

creature_hp(creature)          creature_max_hp(creature)
creature_block(creature)       creature_name(creature)
creature_is_player(creature)   creature_is_alive(creature)
creature_powers(creature)      get_power_amount(creature, power_id)

allies(combat)  enemies(combat)  hittable_enemies(combat)

player_gold(player)      player_energy(player)    player_max_energy(player)
player_stars(player)     player_deck(player)      player_relics(player)
player_potions(player)   player_creature(player)

pile(player, pile_type)  pile_cards(player, pile_type)  all_cards(player)
card_title(card)  card_id(card)  card_cost(card)  card_star_cost(card)  card_type(card)
card_rarity(card) card_upgrade_level(card) card_owner(card)

model_name(handle)  model_id(handle)  log(message)
```

`pile_type` is the `PileType` int: `1` draw, `2` hand, `3` discard, `4` exhaust, `5` play, `6` deck.

## Writing state

```
damage(creature, amount)     lose_hp(creature, amount)   heal(creature, amount)
gain_block(creature, amount) lose_block(creature, amount)

gain_energy(player, n)  lose_energy(player, n)  set_energy(player, n)
gain_stars(player, n)   lose_stars(player, n)
gain_gold(player, n)    lose_gold(player, n)
draw_cards(player, n)

add_card_to_pile(card, pile_type)
create_card(model_id, player) -> card handle
apply_power(power_id, creature, amount)
```

`damage` runs the full damage pipeline: block first, then HP, with every damage hook firing. Model
ids are the strings the game uses internally, for example `"card.strike_ironclad"` or
`"power.vulnerable"`.

## Building a mod

`tools/gdmod/make_gd_mod.py` writes both the PCK and the manifest. The script drives Godot's own
`PCKPacker` rather than `--export-pack`, because the exporter unconditionally adds
`res://.godot/uid_cache.bin` and `res://project.binary` to the archive, and mounting a pack that
carries a UID cache overwrites the game's UID map, breaking every `uid://` reference in the base
game.

```sh
python tools/gdmod/make_gd_mod.py \
     --source tools/gdmod \
     --id example_gd \
     --name "Example GD Mod" \
     --author tester \
     --description "hook bridge smoke test" \
     --version 1.0.0 \
     --out build/mods \
     --godot /path/to/Godot_v4.5.1-stable_mono_win64_console.exe
```

`--out` is a directory: the script creates `build/mods/example_gd/` and writes both files there.
Point `--godot` at a 4.5.x build; `$GODOT` is honoured when the flag is omitted.

## Safe mode

A mod script runs before the main menu, so a mod that throws during startup would otherwise take the
game down with no way back in. The game therefore counts boots: before it loads any mod it writes the
number of consecutive boots that started with mods and never reached the main menu to `user://nomods`,
and a boot that gets to the main menu puts the count back to zero. One such boot is tolerated, because
force-quitting during the loading screen is an ordinary thing to do; the second one suppresses mods.

A boot that runs with mods suppressed is the recovery boot. It switches every mod off in the save file
and clears the counter, so the next launch starts clean and mods can be re-enabled one at a time from
Settings > Modding to find the offender.

To start without mods on purpose, write `manual` into `user://nomods`. On iOS that is
`Documents/nomods`, reachable from the Files app. The game tells a hand-written marker apart from its
own and leaves it alone, so the escape hatch stays until the file is deleted by hand.
