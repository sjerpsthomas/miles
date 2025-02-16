extends Node


const file_name = "user://config.json"


# The MIDI name of the user's keyboard
var keyboard_midi_name: String


# -
func _enter_tree() -> void:
	# open file, get text
	assert(FileAccess.file_exists(file_name), "Create a config.json that contains all attributes!")
	var file := FileAccess.open(file_name, FileAccess.READ)
	var text := file.get_as_text()
	file.close()
	
	# parse and read data
	var data = JSON.parse_string(text)
	assert("keyboard_midi_name" in data, "Unable to find keyboard_midi_name in config!")
	keyboard_midi_name = data["keyboard_midi_name"]

# -
func _exit_tree() -> void:
	# parse and store data
	var data = {
		"keyboard_midi_name" = keyboard_midi_name,
	}
	var text = JSON.stringify(data)
	
	# open file, store text
	var file := FileAccess.open(file_name, FileAccess.WRITE)
	file.store_string(text)
	file.close()
