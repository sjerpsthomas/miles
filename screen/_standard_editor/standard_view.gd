extends Panel


func load_sheet(standard_name: String) -> void:
	$Label.text = standard_name
	
	var file_name := "user://saves/" + standard_name + "/sheet.json"
	var file := FileAccess.open(file_name, FileAccess.READ)
	
	var data = JSON.parse_string(file.get_as_text())
	
	var position_index := 0
	var pickup_measure_count: int = data["PickupMeasureCount"]
	if pickup_measure_count != 0: position_index = 4 - pickup_measure_count
	
	for i in range(data["Chords"].size()):
		var measure_data = data["Chords"][i]
		
		var chords: Array[StandardEditorMeasure.Chord]
		
		for chord_data in measure_data:
			var new_chord := StandardEditorMeasure.Chord.new()
			new_chord.key = chord_data["Key"]
			new_chord.type = chord_data["Type"]
			
			chords.push_back(new_chord)
		
		var measure = preload("res://screen/_standard_editor/measure.tscn").instantiate()
		add_child(measure)
		
		measure.position.x = 128 * (position_index % 4)
		measure.position.y = 64 * (position_index / 4) + 64
		
		measure.add_chords(chords, i < pickup_measure_count)
		
		position_index += 1
	
	size.y = 64 * (position_index / 4) + 64
