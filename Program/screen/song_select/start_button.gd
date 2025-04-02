extends "res://screen/chord_button.gd"


func _pressed() -> void:
	if %TabBar.current_tab == 0:
		goto_performance()
	else:
		goto_recording()


func goto_performance() -> void:
	var standard_view := $"../Standards".get_child(0) as StandardView
	var soloist: int = %SoloistOptionButton.selected
	
	var repetition_count_str: String = %RepetitionsTextEdit.text
	if not repetition_count_str.is_valid_int(): return
	var repetition_count := int(repetition_count_str)
	
	PerformanceScreenInit.notes_path = ""
	PerformanceScreenInit.standard_name = standard_view.standard_name
	PerformanceScreenInit.soloist = soloist
	PerformanceScreenInit.repetition_count = repetition_count
	PerformanceScreenInit.is_pupil = false
	
	get_tree().change_scene_to_file("res://screen/performance/performance_screen.tscn")

func goto_recording() -> void:
	var performance_index: String = %IndexTextEdit.text
	
	var start_measure_str: String = %StartMeasureTextEdit.text
	if not start_measure_str.is_valid_int(): return
	var start_measure := int(start_measure_str)
	
	PerformanceScreenInit.notes_path = "res://recordings/" + performance_index + ".notes"
	PerformanceScreenInit.start_measure = start_measure
	PerformanceScreenInit.standard_name = %OptionsPanel.current_standard
	
	get_tree().change_scene_to_file("res://screen/performance/performance_screen.tscn")
