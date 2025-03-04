extends "res://screen/chord_button.gd"


func navigate() -> void:
	get_tree().change_scene_to_file("res://screen/main/main_screen.tscn")

func _pressed() -> void:
	navigate()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		navigate()
