extends Node
## Example StS2 scripting mod.
##
## Package with tools/gdmod/make_gd_mod.py and drop the resulting folder into the game's mods
## directory:
##   <Documents>/mods/example_gd/
##     example_gd.json      <- manifest, has_pck: true, gd_entry
##     example_gd.pck       <- contains res://example_gd/main.gd
##
## The host instantiates this script once at startup, parents it under GdModHost, and then calls
## every method below that this script actually defines. Delete a method and it is simply skipped —
## there is nothing to register.
##
## Object handles are opaque strings. Pass one back into Sts2 to act on it.

var Sts2

## Set this to a number to add that much flat damage to every attack. null abstains.
var bonus_damage = null

## Set this to a number to multiply attack damage. 1.5 means +50%. null abstains.
var damage_multiplier = null


func _ready() -> void:
	Sts2 = Engine.get_singleton("Sts2")
	if Sts2 == null:
		push_error("example_gd: the Sts2 singleton is not registered")
		return
	Sts2.log("example_gd loaded")


# ---------------------------------------------------------------- lifecycle
#
# The host calls these when the matching C# event fires. Argument counts are fixed: a method that
# declares fewer parameters than the host passes is reported by Godot as "too many arguments" and
# the call is dropped, so keep the signatures as written here.

## run is a handle to the RunState.
func on_run_started(run) -> void:
	Sts2.log("example_gd: run started")


## combat is a handle to the CombatState.
func on_combat_started(combat) -> void:
	Sts2.log("example_gd: combat started")


## combat is a handle to the CombatState.
func on_turn_started(combat) -> void:
	var me = Sts2.get_me()
	if me == "":
		return
	Sts2.log("example_gd: turn start, %d energy, %d gold" % [Sts2.player_energy(me), Sts2.player_gold(me)])


## room is a handle to the CombatRoom.
func on_combat_won(room) -> void:
	Sts2.log("example_gd: combat won")


# ---------------------------------------------------------------- hooks
#
# Every hook below is optional. Returning null means "no opinion" and the game's value is kept.
# Numeric hooks: additive hooks add your return value, multiplicative hooks multiply by it.
# Boolean hooks: return true/false to override, null to keep the game's answer.

## Flat damage added to every attack.
func modify_damage_additive(_target, _amount, _props, _dealer, _card_source):
	return bonus_damage


## Multiplier applied to every attack.
func modify_damage_multiplicative(_target, _amount, _props, _dealer, _card_source):
	return damage_multiplier


## Flat max energy.
func modify_max_energy(_player, _amount):
	return null


## Fires after a card resolves. play is a Dictionary holding a handle to the played CardModel.
func after_card_played(play) -> void:
	var card = play.get("card", "")
	if card == "":
		return
	Sts2.log("example_gd: played %s (%s)" % [Sts2.card_title(card), Sts2.card_id(card)])


## Fires after damage lands on a creature. result is a Dictionary with blocked/unblocked/total.
func after_damage_received(target, result, _props, _dealer, _card_source) -> void:
	var total = result.get("total", 0)
	if total <= 0:
		return
	Sts2.log("example_gd: %s took %d" % [Sts2.creature_name(target), total])


## Return false to keep a creature alive through this hit, true to allow the kill, null to abstain.
func should_die(_creature):
	return null


## Fires at the start of each player turn. player is a handle to the Player.
func after_player_turn_start(player) -> void:
	var creature = Sts2.player_creature(player)
	if creature == "":
		return
	Sts2.log("example_gd: turn start, %d hp / %d max hp" % [Sts2.creature_hp(creature), Sts2.creature_max_hp(creature)])
