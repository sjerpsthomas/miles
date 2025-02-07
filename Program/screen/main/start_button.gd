extends "res://screen/chord_button.gd"


func _pressed() -> void:
	get_tree().change_scene_to_file("res://screen/song_select/song_select_screen.tscn")
