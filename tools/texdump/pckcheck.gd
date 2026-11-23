extends SceneTree

# Mount the freshly built sts2.pck and walk every .import it carries, loading each
# remap target. This is the check that catches "the pack builds but the game is
# blank": textures that never got their .import back, or a .ctex whose header the
# engine cannot parse.
#
# Run:  godot --headless --path <workspace>/ci_bundle/tools/texdump --script pckcheck.gd
#       godot --headless --path ... --script pckcheck.gd -- /path/to/sts2.pck

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
			print("[pck] WORKSPACE NOT FOUND: set STS2_WORKSPACE or pass the pck path")
			quit(1)
			return
		pack = root + "/dist/sts2.pck"

	var ok := ProjectSettings.load_resource_pack(pack, true)
	print("[pck] mount=", ok, " path=", pack)
	if not ok:
		quit(1)
		return

	var imports := _find_imports("res://", [])
	var seen := {}
	var ctex: Array = []
	for imp in imports:
		var target := _remap_path(FileAccess.get_file_as_string(imp))
		if target == "" or seen.has(target):
			continue
		seen[target] = true
		if target.ends_with(".ctex"):
			ctex.append(target)
	print("[pck] unique remap targets=", seen.size(), " ctex=", ctex.size())

	var loaded := 0
	var failed := 0
	var fmt_hist := {}
	var total_px := 0
	for target in ctex:
		var f := FileAccess.open(target, FileAccess.READ)
		if f == null:
			failed += 1
			print("[pck] OPEN FAIL ", target)
			continue
		var head := f.get_buffer(52)
		f.close()
		if head.size() < 52 or head.slice(0, 4).get_string_from_ascii() != "GST2":
			failed += 1
			print("[pck] BAD HEADER ", target)
			continue
		var fmt := head.decode_u32(48)
		fmt_hist[fmt] = fmt_hist.get(fmt, 0) + 1
		var res := ResourceLoader.load(target)
		if res == null:
			failed += 1
			print("[pck] LOAD FAIL ", target)
			continue
		var img = res.get_image()
		if img == null or res.get_size().x <= 0.0:
			failed += 1
			print("[pck] EMPTY ", target)
			continue
		if img.is_compressed():
			if img.decompress() != OK:
				failed += 1
				print("[pck] DECOMPRESS FAIL ", target, " fmt=", fmt)
				continue
		total_px += img.get_width() * img.get_height()
		loaded += 1

	print("[pck] RESULT ok=", loaded, " failed=", failed, " pixels=", total_px)
	print("[pck] fmt histogram ", fmt_hist)
	quit(0 if failed == 0 else 2)


func _remap_path(txt: String) -> String:
	for line in txt.split("\n"):
		var s := line.strip_edges()
		if s.begins_with("path") and s.contains("=\"res://"):
			var a := s.find("\"")
			var b := s.find("\"", a + 1)
			if a >= 0 and b > a:
				return s.substr(a + 1, b - a - 1)
	return ""


func _find_imports(dir: String, acc: Array) -> Array:
	var d := DirAccess.open(dir)
	if d == null:
		return acc
	d.list_dir_begin()
	var name := d.get_next()
	while name != "":
		var full := dir + "/" + name if dir != "res://" else "res://" + name
		if d.current_is_dir():
			if name != "." and name != "..":
				_find_imports(full, acc)
		elif name.ends_with(".import"):
			acc.append(full)
		name = d.get_next()
	d.list_dir_end()
	return acc
