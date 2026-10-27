extends Node
## Example StS2 scripting mod.
##
## Package with tools/gdmod/make_gd_mod.py and drop the resulting folder into the game's mods
## directory:
##   <Documents>/mods/example_gd/
##     example_gd.json      <- manifest, has_pck: true, gd_entry
##     example_gd.pck       <- contains res://example_gd/main.gd
##
## The host instantiates this script once at startup and calls the lifecycle methods below when
## they exist. Everything else is driven by the hook methods: define one and it is called, leave it
## out and it is skipped.
##
## Object handles are opaque strings. Pass one back into Sts2 to act on it.

var Sts2


func _ready() -> void:
	Sts2 = Engine.get_singleton("Sts2")
	if Sts2 == null:
		push_error("example_gd: the Sts2 singleton is not registered")
		return
	Sts2.log("example_gd loaded")


# ---------------------------------------------------------------- lifecycle
#
# The host calls these if present. They take no arguments except the ones noted; the run and combat
# handles are available through Sts2.get_run() / Sts2.get_combat() at any time.

func on_run_started() -> void:
	Sts2.log("example_gd: run started")


func on_combat_started() -> void:
	Sts2.log("example_gd: combat started")


func on_turn_started() -> void:
	Sts2.log("example_gd: turn started")


# ---------------------------------------------------------------- hooks
#
# Every hook below is optional. Returning null means "no opinion" and the game's own value is kept.
# Numeric hooks: additive hooks add your return value, multiplicative hooks multiply by it.
# Boolean hooks: return true/false to override, null to keep the game's answer.

## Adds flat damage to every attack. Return null to abstain.
func modify_damage_additive(_target, _amount, _props, _dealer, _card_source):
	return null


## Multiplies attack damage. Return e.g. 1.5 for +50%, or null to abstain.
func modify_damage_multiplicative(_target, _amount, _props, _dealer, _card_source):
	return null


## Adds flat max energy.
func modify_max_energy(_player, _amount):
	return null


## Fires after a card resolves. play is a Dictionary holding a handle to the played CardModel.
func after_card_played(play) -> void:
	var card = play.get("card", "")
	if card == "":
		return
	Sts2.log("played %s (%s)" % [Sts2.card_title(card), Sts2.card_id(card)])


## Fires after damage lands on a creature. result is a Dictionary with blocked/unblocked/total.
func after_damage_received(target, result, _props, _dealer, _card_source) -> void:
	var total = result.get("total", 0)
	if total <= 0:
		return
	Sts2.log("%s took %d" % [Sts2.creature_name(target), total])


## Return false to make a creature unkillable this hit, null to keep the game's answer.
func should_die(_creature):
	return null


## Fires at the start of each player turn.
func after_player_turn_start(player) -> void:
	Sts2.log("turn start: %d energy, %d gold" % [Sts2.player_energy(player), Sts2.player_gold(player)])
