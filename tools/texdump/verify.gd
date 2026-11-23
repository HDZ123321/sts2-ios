extends SceneTree

# Spot-check a sample of the recooked textures: parse the container by hand, hand the
# payload to Image.create_from_data, and assert the format is the one the iOS preset
# would pick. Unlike recook.gd this never writes anything.
#
# The sample comes from _verify.json (a dozen or so entries covering both modes); it is
# optional, and make_plan.py --verify-out regenerates it from _plan.json.
#
# Run:  godot --headless --path <workspace>/ci_bundle/tools/texdump --script verify.gd

func _find_workspace() -> String:
	var override := OS.get_environment("STS2_WORKSPACE")
	if override != "":
		return override.trim_suffix("/")
	var dir := ProjectSettings.globalize_path("res://").trim_suffix("/")
	for _i in 6:
		if DirAccess.dir_exists_absolute(dir + "/pck113"):
			return dir
		dir = dir.get_base_dir()
	return ""


func _init() -> void:
	var root := _find_workspace()
	if root == "":
		print("WORKSPACE NOT FOUND: set STS2_WORKSPACE")
		quit(1)
		return
	var dst_dir := root + "/pck113_tex/.godot/imported/"
	var plan_path := ProjectSettings.globalize_path("res://_verify.json")

	var plan = JSON.parse_string(FileAccess.get_file_as_string(plan_path))
	if plan == null:
		print("VERIFY PLAN FAIL ", plan_path, " (run make_plan.py --verify-out)")
		quit(1)
		return
	print("verify items: ", plan.size(), " workspace=", root)

	var ok := 0
	var bad := 0
	for item in plan:
		var p: String = dst_dir + item["dst"]
		var f := FileAccess.open(p, FileAccess.READ)
		if f == null:
			print("MISSING ", p)
			bad += 1
			continue
		var size := f.get_length()
		var head := f.get_buffer(52)
		if head.size() < 52 or head.slice(0, 4).get_string_from_ascii() != "GST2":
			print("BAD HEADER ", p)
			f.close()
			bad += 1
			continue
		var w := head.decode_u16(40)
		var h := head.decode_u16(42)
		var mips := head.decode_u32(44)
		var fmt := head.decode_u32(48)
		var payload := f.get_buffer(size - 52)
		f.close()

		var img := Image.create_from_data(w, h, mips > 0, fmt, payload)
		if img == null or img.is_empty():
			print("BAD create ", p)
			bad += 1
			continue
		if img.decompress() != OK:
			print("BAD decompress ", p)
			bad += 1
			continue

		# ASTC 4x4 is format 35; ETC2 is 31 (RGBA8) or 30 (RGB8).
		if item["mode"] == "astc":
			if fmt != 35:
				print("BAD astc fmt ", fmt, " ", p)
				bad += 1
				continue
		else:
			if fmt != 31 and fmt != 30:
				print("BAD etc2 fmt ", fmt, " ", p)
				bad += 1
				continue
		print("OK ", item["mode"], " ", w, "x", h, " mips=", mips, " -> fmt=", img.get_format(),
			" rgba=", img.get_data().size())
		ok += 1

	print("verify ok=", ok, " bad=", bad)
	quit(0 if bad == 0 else 2)
