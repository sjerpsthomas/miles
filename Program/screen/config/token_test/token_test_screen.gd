extends Node2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	get_viewport().files_dropped.connect(_on_files_dropped)


func _on_files_dropped(files: PackedStringArray) -> void:
	if files.size() != 1: return
	
	print(files)
	%TokenLoader.LoadTokens(files[0])


func _on_stop_playing_button_pressed() -> void:
	%TokenLoader.StopPlaying()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"):
		get_tree().change_scene_to_file("res://screen/config/config_screen.tscn")
