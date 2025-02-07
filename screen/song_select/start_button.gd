extends "res://screen/chord_button.gd"


func _pressed() -> void:
	var standard_view := $"../Standards".get_child(0) as StandardView
	var soloist: int = %SoloistOptionButton.selected
	
	var repetition_count_str: String = %RepetitionsTextEdit.text
	if not repetition_count_str.is_valid_int(): return
	var repetition_count := int(repetition_count_str)
	
	PerformanceScreenInit.standard_name = standard_view.standard_name
	PerformanceScreenInit.soloist = soloist
	PerformanceScreenInit.repetition_count = repetition_count
	
	get_tree().change_scene_to_file("res://screen/performance/performance_screen.tscn")
