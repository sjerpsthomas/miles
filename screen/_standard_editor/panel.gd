extends Panel


@export var standard_view: Control

var measure_num: int = -1


func _ready() -> void:
	init.call_deferred()

func init() -> void:
	$ExtraOptions/RhythmTypeOptionButton.select(standard_view.data["Style"])

func _on_standard_view_measure_clicked(new_measure_num: int) -> void:
	for chord_edit in $Chords.get_children():
		chord_edit.queue_free()
	
	measure_num = new_measure_num
	
	var measure_data = standard_view.data["Chords"][measure_num]
	for chord in measure_data:
		add_chord(chord["Key"], chord["Type"])
	
	$AddChordButton.visible = measure_num != -1
	
	refresh()

func add_chord(key: StandardEditorMeasure.KeyEnum, type: StandardEditorMeasure.TypeEnum) -> void:
	var new_chord_edit := preload("res://screen/_standard_editor/chord_edit/chord_edit.tscn").instantiate()
	
	$Chords.add_child(new_chord_edit)
	
	new_chord_edit.initialize(key, type)
	
	new_chord_edit.position.x = 16
	
	new_chord_edit.value_changed.connect(apply_to_standard_view)
	new_chord_edit.deleted.connect(_on_ChordEdit_deleted.bind(new_chord_edit))

func _on_ChordEdit_deleted(chord_edit: Node) -> void:
	chord_edit.queue_free()
	refresh()
	apply_to_standard_view()

func refresh() -> void:
	var chord_edits := $Chords.get_children()
	
	var i := 0
	
	for chord_edit in chord_edits:
		if chord_edit.is_queued_for_deletion(): continue
		
		chord_edit.position.y = 16 + 112 * i
		
		i += 1
	
	if i == 0:
		size.y = 140
	elif i == 1:
		size.y = 188
	else:
		size.y = 188 + 112 * (i - 1)

func apply_to_standard_view() -> void:
	var new_measure_data := []
	
	for chord_edit in $Chords.get_children():
		if chord_edit.is_queued_for_deletion(): continue
		
		new_measure_data.push_back({ "Key": chord_edit.key, "Type": chord_edit.type })
	
	standard_view.data["Chords"][measure_num] = new_measure_data
	
	standard_view.data["Style"] = $ExtraOptions/RhythmTypeOptionButton.selected
	
	standard_view.refresh()


func _on_add_chord_button_pressed() -> void:
	add_chord(StandardEditorMeasure.KeyEnum.C, StandardEditorMeasure.TypeEnum.Major)
	refresh()
	apply_to_standard_view()


func _on_rhythm_type_option_button_item_selected(_index: int) -> void:
	apply_to_standard_view()
