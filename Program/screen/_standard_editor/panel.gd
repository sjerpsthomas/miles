extends Panel


@onready var standard_view := %StandardView as StandardView

var measure_num: int = -1


# -
func _ready() -> void:
	_on_standard_view_measure_clicked(-1)

# sets measure number @ StandardView measure_clicked / ui_cancel
func _on_standard_view_measure_clicked(new_measure_num: int) -> void:
	# set measure number
	if new_measure_num == measure_num:
		measure_num = -1
	else:
		measure_num = new_measure_num
	
	# show/hide elements
	var measure_selected := measure_num != -1
	$SectionLabelLabel.visible = measure_selected
	$SectionLabelTextEdit.visible = measure_selected
	$AddChordButton.visible = measure_selected
	%CurrentChord.visible = measure_selected
	
	# delete old chord edits
	for chord_edit in $Chords.get_children(): chord_edit.queue_free()
	
	if measure_selected:
		# set highlight position
		%CurrentChord.global_position = standard_view.get_measure_global_position(measure_num)
		
		# create new chord edits
		var measure_data = standard_view.data["Chords"][measure_num]
		for chord in measure_data: create_chord_edit(chord["Key"], chord["Type"])
		
		# set section label text
		$SectionLabelTextEdit.text = standard_view.data["SectionLabels"][measure_num]
	
	refresh()

# creates a chord edit UI component
func create_chord_edit(key: StandardEditorMeasure.KeyEnum, type: StandardEditorMeasure.TypeEnum) -> void:
	# create chord edit, add as child
	var new_chord_edit := preload("res://screen/_standard_editor/chord_edit/chord_edit.tscn").instantiate()
	$Chords.add_child(new_chord_edit)
	new_chord_edit.initialize(key, type)
	
	# set position, connect signals
	new_chord_edit.position.x = 16
	new_chord_edit.value_changed.connect(_on_chord_edit_value_changed)
	new_chord_edit.deleted.connect(_on_chord_edit_deleted.bind(new_chord_edit))

# refreshes the chord layout and size
func refresh() -> void:
	# position not-queued chord edits
	var i := 0
	for chord_edit in $Chords.get_children():
		if chord_edit.is_queued_for_deletion(): continue
		chord_edit.position.y = 16 + 112 * i
		i += 1
	
	# set height
	if i == 0:
		if measure_num == -1: custom_minimum_size.y = 0
		else: custom_minimum_size.y = 140 + 80
	elif i == 1: custom_minimum_size.y = 188 + 80
	else: custom_minimum_size.y = 188 + 80 + 112 * (i - 1)

# applies the UI data to the standard view
func apply_to_standard() -> void:
	# early return if no measure selected
	if measure_num == -1: return
	
	# apply data of not-queued chord edits
	var new_measure_data := []
	for chord_edit in $Chords.get_children():
		if not chord_edit.is_queued_for_deletion():
			new_measure_data.push_back({ "Key": chord_edit.key, "Type": chord_edit.type })
	
	standard_view.data["Chords"][measure_num] = new_measure_data
	
	# apply data of section label
	var new_section_label: String = $SectionLabelTextEdit.text
	standard_view.data["SectionLabels"][measure_num] = new_section_label
	$SectionLabelTextEdit.flat = new_section_label != ""

# -
func _unhandled_input(event: InputEvent) -> void:
	# simulate deselecting when pressing escape
	if event.is_action_pressed("ui_cancel") and measure_num != -1:
		_on_standard_view_measure_clicked(-1)
		get_viewport().set_input_as_handled()

# create chord and apply data @ AddChordButton pressed
func _on_add_chord_button_pressed() -> void:
	# create chord
	create_chord_edit(StandardEditorMeasure.KeyEnum.C, StandardEditorMeasure.TypeEnum.Major)
	
	refresh()
	%UI.apply_all_to_standard()

# delete chord and apply data @ ChordEdit deleted
func _on_chord_edit_deleted(chord_edit: Node) -> void:
	# delete chord edit component
	chord_edit.queue_free()
	
	refresh()
	%UI.apply_all_to_standard()

# apply data @ ChordEdit value_changed
func _on_chord_edit_value_changed() -> void:
	%UI.apply_all_to_standard()

# apply data @ SectionLabelTextEdit text_changed
func _on_section_label_text_edit_text_changed(_new_text: String) -> void:
	%UI.apply_all_to_standard()
