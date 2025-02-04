class_name StandardEditorMeasure
extends NinePatchRect


enum KeyEnum { C = 0, Db, D, Eb, E, F, Gb, G, Ab, A, Bb, B }

enum TypeEnum
{
	Major = 0,
	Dominant,
	Minor,
	HalfDim7,
}

class Chord:
	var key: KeyEnum
	var type: TypeEnum
	
	func get_type_string() -> String:
		var type_str: String
		
		match type:
			TypeEnum.Major: type_str = "Δ7"
			TypeEnum.Dominant: type_str = "7"
			TypeEnum.Minor: type_str = "m7"
			TypeEnum.HalfDim7: type_str = "m7b5"
		
		return type_str
	
	func _to_string() -> String:
		return KeyEnum.find_key(key) + get_type_string()
	
	func to_bbcode() -> String:
		return "[center]" + KeyEnum.find_key(key) + "[font_size=20]" + get_type_string()


signal clicked


# initializes the measure
func initialize(new_chords: Array[Chord], section_label: String, double_barline: bool) -> void:
	# initialize section label and barline
	$SectionLabel.text = section_label
	$DoubleBarline.visible = double_barline
	
	# get width of chord
	var chord_width := size.x / new_chords.size()
	
	# add chords
	for i in range(new_chords.size()):
		var chord := new_chords[i]
		
		# instantiate chord
		var chord_instance := preload("res://standard_view/chord.tscn").instantiate()
		add_child(chord_instance)
		
		# set chord size and position
		chord_instance.set_deferred("size", Vector2(chord_width, chord_instance.size.y))
		chord_instance.position.x = i * chord_width
		
		# set chord text
		chord_instance.get_node("Label").text = chord.to_bbcode()

# passes signal @ self gui_input
func _on_gui_input(event: InputEvent) -> void:
	if event.is_action_pressed("click"):
		clicked.emit()
