extends Node2D


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		get_tree().change_scene_to_file("res://screen/main/main_screen.tscn")

func _on_token_test_screen_button_pressed() -> void:
	get_tree().change_scene_to_file("res://screen/config/token_test/token_test_screen.tscn")
