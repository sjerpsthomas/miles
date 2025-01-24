class_name StandardView
extends Panel


var standard_name: String

var data

signal measure_clicked(measure_num: int)


func load_sheet(new_standard_name: String) -> void:
	standard_name = new_standard_name
	$Label.text = standard_name
	
	var file_name := "user://saves/" + standard_name + "/sheet.json"
	var file := FileAccess.open(file_name, FileAccess.READ)
	
	data = JSON.parse_string(file.get_as_text())
	
	file.close()
	
	refresh()


func save_sheet() -> void:
	var file_name := "user://saves/" + standard_name + "/sheet.json"
	var file := FileAccess.open(file_name, FileAccess.WRITE)
	
	var text = JSON.stringify(data, "  ")
	
	file.store_string(text)
	
	file.close()


func refresh() -> void:
	for measure in $Measures.get_children():
		measure.queue_free()
	
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
		$Measures.add_child(measure)
		
		measure.position.x = 192 * (position_index % 4)
		measure.position.y = 64 * (position_index / 4) + 64
		
		measure.is_pickup_measure = i < pickup_measure_count
		measure.initialize(chords)
		
		measure.clicked.connect(_on_measure_clicked.bind(i))
		
		position_index += 1
	
	size.y = 64 * (position_index / 4) + 64
	custom_minimum_size.y = size.y


func _on_measure_clicked(measure_num: int) -> void:
	measure_clicked.emit(measure_num)
