extends Node2D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	var standard_names := DirAccess.get_directories_at("user://saves/")
	
	for i in standard_names.size():
		var standard_name := standard_names[i]
		
		var standard_view := preload("res://screen/_standard_editor/standard_view.tscn").instantiate()
		add_child(standard_view)
		
		standard_view.position.x = 720 * i
		standard_view.load_sheet(standard_name)
