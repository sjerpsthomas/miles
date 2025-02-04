class_name StandardView
extends Panel


@export var height := 60.0
@export var fade_chords := false

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
	
	var section_num := 0
	
	var num_chords: int = data["Chords"].size()
	for i in range(num_chords):
		var measure_data = data["Chords"][i]
		var solo_division: int = data["SoloDivision"][i]
		var section_label: String = data["SectionLabels"][i]
		var double_barline: bool = i == num_chords - 1 or data["SectionLabels"][i + 1] != ""
		
		var chords: Array[StandardEditorMeasure.Chord]
		
		for chord_data in measure_data:
			var new_chord := StandardEditorMeasure.Chord.new()
			new_chord.key = chord_data["Key"]
			new_chord.type = chord_data["Type"]
			
			chords.push_back(new_chord)
		
		var measure: NinePatchRect = preload("res://standard_view/measure.tscn").instantiate()
		$Measures.add_child(measure)
		
		measure.global_position = get_measure_global_position(position_index)
		measure.size.y = height
		
		measure.is_pickup_measure = i < pickup_measure_count
		measure.initialize(chords, section_label, double_barline)
		
		if solo_division == 1:
			#measure.modulate = Color.GAINSBORO
			measure.modulate = Color.CORNFLOWER_BLUE.lightened(0.7)
			measure.modulate.a = 0.6
		
		measure.clicked.connect(_on_measure_clicked.bind(i))
		
		position_index += 1
	
	size.y = height * (position_index / 4) + height
	custom_minimum_size.y = size.y


func get_measure_global_position(position_index: int) -> Vector2:
	return global_position + Vector2(192 * (position_index % 4), height * (position_index / 4) + 48)


func _on_measure_clicked(measure_num: int) -> void:
	measure_clicked.emit(measure_num)
