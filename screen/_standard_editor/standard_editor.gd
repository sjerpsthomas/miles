class_name StandardEditor
extends Node2D


static var standard_name: String = "Needlessly Long"

@export var standard_view: Node


func _ready() -> void: load_standard()

func load_standard() -> void:
	assert(standard_name != "")
	
	standard_view.load_sheet(standard_name)

func _on_save_and_return_button_pressed() -> void:
	standard_view.save_sheet()
	get_tree().change_scene_to_file("res://screen/song_select/song_select_screen.tscn")

func _on_discard_changes_button_pressed() -> void:
	get_tree().change_scene_to_file("res://screen/song_select/song_select_screen.tscn")
