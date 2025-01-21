extends "res://screen/chord_button.gd"


func press() -> void:
	get_tree().change_scene_to_file("res://scene.tscn")
