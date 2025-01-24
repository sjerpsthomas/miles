extends "res://screen/chord_button.gd"


func _pressed() -> void:
	var standard_view := $"../Standards".get_child(0) as StandardView
	PerformanceScreenInit.standard_name = standard_view.standard_name
	
	get_tree().change_scene_to_file("res://screen/performance/performance_screen.tscn")
