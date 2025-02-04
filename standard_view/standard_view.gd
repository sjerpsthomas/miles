class_name StandardView
extends Panel


@export var height := 60.0
@export var fade_chords := false

var standard_name: String

var data


signal measure_clicked(measure_num: int)

# loads the given standard name
func load_sheet(new_standard_name: String) -> void:
	assert(new_standard_name != "")
	standard_name = new_standard_name
	
	# set standard title
	$Label.text = standard_name
	
	# open file, get text
	var file_name := "user://saves/" + standard_name + "/sheet.json"
	var file := FileAccess.open(file_name, FileAccess.READ)
	var text := file.get_as_text()
	file.close()
	
	# parse JSON into data
	data = JSON.parse_string(text)
	
	# refresh
	refresh()

# saves the standard to file
func save_sheet() -> void:
	# serialize data
	var text = JSON.stringify(data, "  ")
	
	# open file, store text
	var file_name := "user://saves/" + standard_name + "/sheet.json"
	var file := FileAccess.open(file_name, FileAccess.WRITE)
	file.store_string(text)
	file.close()

# refreshes all content of the standard view
func refresh() -> void:
	# free all measures
	for measure in $Measures.get_children():
		measure.queue_free()
	
	# keep track of measure number
	var measure_num := 0
	
	# initialize measures
	var num_measures: int = data["Chords"].size()
	for i in range(num_measures):
		var measure_data = data["Chords"][i]
		var solo_division: int = data["SoloDivision"][i]
		var section_label: String = data["SectionLabels"][i]
		var double_barline: bool = i == num_measures - 1 or data["SectionLabels"][i + 1] != ""
		
		var chords: Array[StandardEditorMeasure.Chord]
		
		for chord_data in measure_data:
			var new_chord := StandardEditorMeasure.Chord.new()
			new_chord.key = chord_data["Key"]
			new_chord.type = chord_data["Type"]
			
			chords.push_back(new_chord)
		
		# initialize measure
		var measure: NinePatchRect = preload("res://standard_view/measure.tscn").instantiate()
		$Measures.add_child(measure)
		measure.initialize(chords, section_label, double_barline)
		
		# connect measure signal
		measure.clicked.connect(_on_measure_clicked.bind(i))
		
		# set measure position
		measure.global_position = get_measure_global_position(measure_num)
		measure.size.y = height
		
		# set measure color
		if solo_division == 1:
			measure.modulate = Color.CORNFLOWER_BLUE.lightened(0.7)
			measure.modulate.a = 0.6
		
		measure_num += 1
	
	# set size based on number of measures
	size.y = height * (measure_num / 4) + height
	custom_minimum_size.y = size.y

# gets the global position for the given measure number
func get_measure_global_position(measure_num: int) -> Vector2:
	return global_position + Vector2(192 * (measure_num % 4), height * (measure_num / 4) + 48)

# pass signal @ measure clicked
func _on_measure_clicked(measure_num: int) -> void:
	measure_clicked.emit(measure_num)
