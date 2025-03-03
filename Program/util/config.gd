extends Node


const file_name = "user://config.json"


# The MIDI name of the user's keyboard
var keyboard_midi_name: String

var pupil_info: Dictionary


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
	
	assert("pupil_info" in data, "Unable to find pupil_info in config!")
	pupil_info = data["pupil_info"]

# -
func _exit_tree() -> void:
	# parse and store data
	var data = {
		"keyboard_midi_name" = keyboard_midi_name,
		"pupil_info" = pupil_info,
	}
	var text = JSON.stringify(data, '\t')
	
	# open file, store text
	var file := FileAccess.open(file_name, FileAccess.WRITE)
	file.store_string(text)
	file.close()
