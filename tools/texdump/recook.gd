extends SceneTree

# Recook the desktop GPU texture variants into the two the iOS exporter can ship.
#
#   .bptc.ctex (BPTC / BC7)   -> .astc.ctex (ASTC 4x4, format 35)
#   .s3tc.ctex (DXT5 / DXT1)  -> .etc2.ctex (ETC2 RGBA8 / RGB8, format 31 / 30)
#
# Godot's iOS preset declares the feature set {etc2, astc, arm64}
# (editor/export/editor_export_platform_apple_embedded.cpp), so the exporter picks
# whichever variant exists. Neither exists in the desktop pack, which is why the port
# synthesises them.
#
# Workspace layout this expects (see tools/texdump/README.md):
#   <workspace>/pck113/             unpacked v0.113.1 tree
#   <workspace>/pck113_tex/         output tree, mirrors pck113/
#   <workspace>/ci_bundle/tools/texdump/   this project (res://)
#
# The workspace is found by walking up from res:// until a directory with pck113
# shows up, so the checkout can live anywhere. Override with STS2_WORKSPACE.
#
# Run:  godot --headless --path <workspace>/ci_bundle/tools/texdump --script recook.gd
# Re-runs skip textures already present, so an interrupted run just continues.

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
		print("WORKSPACE NOT FOUND: no pck113 above res://, set STS2_WORKSPACE")
		quit(1)
		return
	var src_dir := root + "/pck113/.godot/imported/"
	var dst_dir := root + "/pck113_tex/.godot/imported/"
	var plan_path := ProjectSettings.globalize_path("res://_plan.json")

	var plan = JSON.parse_string(FileAccess.get_file_as_string(plan_path))
	if plan == null:
		print("PLAN FAIL ", plan_path, " (run make_plan.py first)")
		quit(1)
		return
	print("plan items: ", plan.size(), " workspace=", root)

	var done := 0
	var failed := 0
	var astc_n := 0
	var etc2_n := 0
	DirAccess.make_dir_recursive_absolute(dst_dir)

	for item in plan:
		var src_path: String = src_dir + item["src"]
		var dst_path: String = dst_dir + item["dst"]
		if FileAccess.file_exists(dst_path):
			done += 1
			continue
		var f := FileAccess.open(src_path, FileAccess.READ)
		if f == null:
			print("OPEN FAIL ", src_path)
			failed += 1
			continue
		var size := f.get_length()
		var head := f.get_buffer(52)
		# 36-byte container header (GST2 + version + w/h/df/mipmap_limit + 3 reserved)
		# then the 16-byte image header: data_format, w, h, mipmaps, format.
		var df := head.decode_u32(16)
		var mmlim := head.decode_u32(20)
		var w := head.decode_u16(40)
		var h := head.decode_u16(42)
		var mips := head.decode_u32(44)
		var fmt := head.decode_u32(48)
		var payload := f.get_buffer(size - 52)
		f.close()

		var img := Image.create_from_data(w, h, mips > 0, fmt, payload)
		if img == null or img.is_empty():
			print("IMAGE FAIL ", src_path)
			failed += 1
			continue
		if img.is_compressed():
			if img.decompress() != OK:
				print("DECOMP FAIL ", src_path)
				failed += 1
				continue

		var out := img.duplicate()
		var mode := Image.COMPRESS_ASTC if item["mode"] == "astc" else Image.COMPRESS_ETC2
		if out.compress(mode) != OK:
			print("COMPRESS FAIL ", src_path)
			failed += 1
			continue
		if item["mode"] == "astc":
			astc_n += 1
		else:
			etc2_n += 1

		# Rebuild the container: same 36-byte header, same mipmap count, only the
		# format field and the payload change.
		var body := out.get_data()
		var outf := FileAccess.open(dst_path, FileAccess.WRITE)
		if outf == null:
			print("WRITE FAIL ", dst_path)
			failed += 1
			continue
		outf.store_buffer("GST2".to_utf8_buffer())
		outf.store_32(1)
		outf.store_32(w)
		outf.store_32(h)
		outf.store_32(df)
		outf.store_32(mmlim)
		outf.store_32(0)
		outf.store_32(0)
		outf.store_32(0)
		outf.store_32(0)
		outf.store_16(w)
		outf.store_16(h)
		outf.store_32(mips)
		outf.store_32(out.get_format())
		outf.store_buffer(body)
		outf.close()
		done += 1
		if done % 100 == 0:
			print("  progress ", done, "/", plan.size())

	print("done=", done, " failed=", failed, " astc=", astc_n, " etc2=", etc2_n)
	quit(0 if failed == 0 else 2)
