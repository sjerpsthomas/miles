extends ScrollContainer


var index := 0

@onready var standard_names: PackedStringArray


func _ready() -> void:
	standard_names = DirAccess.get_directories_at("user://saves/")
	refresh()


func refresh() -> void:
	var standard_name := standard_names[index]
	
	var standard_view := preload("res://screen/_standard_editor/standard_view.tscn").instantiate()
	add_child(standard_view)
	
	standard_view.load_sheet(standard_name)


func _on_left_button_pressed() -> void:
	index = posmod(index - 1, standard_names.size())
	refresh()


func _on_right_button_pressed() -> void:
	index = posmod(index + 1, standard_names.size())
	refresh()
