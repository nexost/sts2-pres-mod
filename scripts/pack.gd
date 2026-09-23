# Packs an explicit file list into a Godot PCK using the engine's own PCKPacker.
# Run: godot --headless --path mod --script <this file> -- --list=<list.txt> --out=<file.pck>
# Each list line: <res path without res://>|<absolute source path>
extends SceneTree


func _init() -> void:
	var list_path := ""
	var out_path := ""
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--list="):
			list_path = arg.substr(7)
		elif arg.begins_with("--out="):
			out_path = arg.substr(6)
	if list_path == "" or out_path == "":
		push_error("pack.gd: need --list=<file> and --out=<file.pck>")
		quit(2)
		return

	var packer := PCKPacker.new()
	var err := packer.pck_start(out_path)
	if err != OK:
		push_error("pack.gd: pck_start failed: %s" % err)
		quit(3)
		return

	var count := 0
	var list := FileAccess.open(list_path, FileAccess.READ)
	while not list.eof_reached():
		var line := list.get_line().strip_edges()
		if line == "":
			continue
		var parts := line.split("|", false, 1)
		err = packer.add_file("res://" + parts[0], parts[1])
		if err != OK:
			push_error("pack.gd: add_file failed for %s: %s" % [parts[0], err])
			quit(4)
			return
		count += 1

	err = packer.flush(false)
	if err != OK:
		push_error("pack.gd: flush failed: %s" % err)
		quit(5)
		return
	print("pack.gd: packed %d files into %s" % [count, out_path])
	quit(0)
