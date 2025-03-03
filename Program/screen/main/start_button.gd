extends "res://screen/chord_button.gd"


func _pressed() -> void:
	var pupil_info: String = %PupilInfoLineEdit.text
	
	if pupil_info == "":
		get_tree().change_scene_to_file("res://screen/song_select/song_select_screen.tscn")
	else:
		var pupil_info_arr := pupil_info.split(' ')
		var pupil_info_name := pupil_info_arr[0]
		var pupil_info_index := int(pupil_info_arr[1])
		
		var config_dict: Dictionary = Config.pupil_info[pupil_info_name][pupil_info_index]
		
		var standard_name = config_dict["standard"]
		var soloist = config_dict["soloist"]
		
		PerformanceScreenInit.standard_name = standard_name
		PerformanceScreenInit.soloist = soloist
		PerformanceScreenInit.repetition_count = 2
		PerformanceScreenInit.is_pupil = true
		
		print("[MAIN_SCREEN] Playing ", standard_name, " with soloist ", soloist)
		
		get_tree().change_scene_to_file("res://screen/performance/performance_screen.tscn")
