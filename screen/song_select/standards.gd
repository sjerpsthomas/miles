extends ScrollContainer


var index := 0

@onready var standard_names: PackedStringArray


func _ready() -> void:
	standard_names = DirAccess.get_directories_at("user://saves/")
	refresh()


func refresh() -> void:
	var standard_name := standard_names[index]
	
	for child in get_children():
		child.queue_free()
	
	var standard_view: StandardView = preload("res://standard_view/standard_view.tscn").instantiate()
	add_child(standard_view)
	
	standard_view.load_sheet(standard_name)


func navigate(direction: int) -> void:
	index = posmod(index + direction, standard_names.size())
	refresh()
