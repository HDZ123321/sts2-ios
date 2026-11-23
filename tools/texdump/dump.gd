extends SceneTree

# Enumerate what a mounted pack actually carries, without loading anything heavy:
# counts by extension plus the .ctex format histogram. Useful before/after a rebuild to
# confirm the recook replaced the desktop variants (BPTC=22 / DXT5=23 / DXT1=24 give way
# to ASTC=35 and ETC2=30/31).
#
# Run:  godot --headless --path <workspace>/ci_bundle/tools/texdump --script dump.gd
#       godot --headless --path ... --script dump.gd -- /path/to/sts2.pck

var _ext_hist := {}
var _fmt_hist := {}
var _ctex_count := 0
var _ctex_bytes := 0


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
			print("WORKSPACE NOT FOUND: set STS2_WORKSPACE or pass the pck path")
			quit(1)
			return
		pack = root + "/dist/sts2.pck"

	var ok := ProjectSettings.load_resource_pack(pack, true)
	print("mount=", ok, " path=", pack)
	if not ok:
		quit(1)
		return

	_walk("res://")

	var pairs := []
	for key in _ext_hist:
		pairs.append([_ext_hist[key], key])
	pairs.sort_custom(func(a, b): return a[0] > b[0])
	print("extensions (top 25 of ", pairs.size(), "):")
	var shown := 0
	for p in pairs:
		if shown >= 25:
			break
		print("  ", p[1], " ", p[0])
		shown += 1
	print("ctex total=", _ctex_count, " bytes=", _ctex_bytes)
	print("ctex format histogram ", _fmt_hist)
	quit(0)


func _walk(dir: String) -> void:
	var d := DirAccess.open(dir)
	if d == null:
		return
	d.list_dir_begin()
	var name := d.get_next()
	while name != "":
		if name != "." and name != "..":
			var full: String = dir + name
			if d.current_is_dir():
				_walk(full + "/")
			else:
				var ext := name.get_extension()
				if ext == "":
					ext = "(none)"
				_ext_hist[ext] = _ext_hist.get(ext, 0) + 1
				if ext == "ctex":
					_count_ctex(full)
		name = d.get_next()
	d.list_dir_end()


func _count_ctex(path: String) -> void:
	_ctex_count += 1
	var f := FileAccess.open(path, FileAccess.READ)
	if f == null:
		return
	_ctex_bytes += f.get_length()
	var head := f.get_buffer(52)
	if head.size() >= 52:
		var fmt := head.decode_u32(48)
		_fmt_hist[fmt] = _fmt_hist.get(fmt, 0) + 1
	f.close()
