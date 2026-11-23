extends SceneTree

# Second-pass check on a built pack: load every resource that has an .import, not just
# the .ctex ones. Catches .spatlas / .spskel / .fontdata / scene remaps that pckcheck.gd
# does not look at.
#
# Run:  godot --headless --path <workspace>/ci_bundle/tools/texdump --script remapcheck.gd
#       godot --headless --path ... --script remapcheck.gd -- /path/to/sts2.pck

func _find_workspace() -> String:
	var override := OS.get_environment("STS2_WORKSPACE")
	if override != "":
		return override.trim_suffix("/")
	var dir := ProjectSettings.globalize_path("res://").trim_suffix("/")
	for _i in 6:
		if DirAccess.dir_exists_absolute(dir + "/dist"):
			return dir
		dir = dir.get_base_dir()
	return ""


func _init() -> void:
	var pack := ""
	for arg in OS.get_cmdline_user_args():
		pack = arg
	if pack == "":
		var root := _find_workspace()
		if root == "":
			print("[remap] WORKSPACE NOT FOUND: set STS2_WORKSPACE or pass the pck path")
			quit(1)
			return
		pack = root + "/dist/sts2.pck"

	var ok := ProjectSettings.load_resource_pack(pack, true)
	print("[remap] mount=", ok, " path=", pack)
	if not ok:
		quit(1)
		return
	print("[remap] platform=", OS.get_name(),
		" etc2_astc=", ProjectSettings.get_setting("rendering/textures/vram_compression/import_etc2_astc", "unset"))

	var imports := _find_imports("res://", [])
	print("[remap] imports=", imports.size())
	var loaded := 0
	var failed := 0
	var kinds := {}
	var shown := 0
	for imp in imports:
		var src: String = imp.substr(0, imp.length() - ".import".length())
		var res := ResourceLoader.load(src)
		if res == null:
			failed += 1
			if shown < 15:
				print("[remap] FAIL ", src)
				shown += 1
			continue
		loaded += 1
		var cls := res.get_class()
		kinds[cls] = kinds.get(cls, 0) + 1
	print("[remap] RESULT ok=", loaded, " failed=", failed, " classes=", kinds)
	quit(0 if failed == 0 else 2)


func _find_imports(dir: String, acc: Array) -> Array:
	var d := DirAccess.open(dir)
	if d == null:
		return acc
	d.list_dir_begin()
	var name := d.get_next()
	while name != "":
		if name != "." and name != "..":
			var full := dir + name
			if d.current_is_dir():
				_find_imports(full + "/", acc)
			elif name.ends_with(".import"):
				acc.append(full)
		name = d.get_next()
	d.list_dir_end()
	return acc
