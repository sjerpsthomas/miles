extends "res://screen/chord_button.gd"


func _pressed() -> void:
	var standard_view := $"../Standards".get_child(0) as StandardView
	var soloist: int = $"../SoloistOptionButton".selected
	
	PerformanceScreenInit.standard_name = standard_view.standard_name
	PerformanceScreenInit.soloist = soloist
	
	get_tree().change_scene_to_file("res://screen/performance/performance_screen.tscn")
