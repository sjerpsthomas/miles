extends Button


func _pressed() -> void:
	var standards := $"../Standards"
	StandardEditor.standard_name = standards.standard_names[standards.index]
	get_tree().change_scene_to_file("res://screen/_standard_editor/standard_editor.tscn")
