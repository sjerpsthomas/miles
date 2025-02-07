class_name StandardEditor
extends Node2D


static var standard_name: String = ""


# -
func _ready() -> void:
	# load standard
	%StandardView.load_sheet(standard_name)

# save standard and return @ SaveAndReturnButton pressed
func _on_save_and_return_button_pressed() -> void:
	%StandardView.save_sheet()
	get_tree().change_scene_to_file("res://screen/song_select/song_select_screen.tscn")

# discard and return @ DiscardChangesButton pressed
func _on_discard_changes_button_pressed() -> void:
	get_tree().change_scene_to_file("res://screen/song_select/song_select_screen.tscn")
