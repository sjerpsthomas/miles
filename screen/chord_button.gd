extends Button


@export var chord: String
var keys_pressed: bool



func _on_main_screen_press() -> void:
	var counts_string: String = get_parent().CountsString
	var new_keys_pressed := counts_string == chord
	
	if keys_pressed == new_keys_pressed: return
	keys_pressed = new_keys_pressed
	
	if keys_pressed:
		_pressed()
