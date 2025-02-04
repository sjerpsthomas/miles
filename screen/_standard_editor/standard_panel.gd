extends Panel


@onready var standard_view := %StandardView as StandardView


# -
func _ready() -> void:
	init.call_deferred()

# TODO kan niet in _ready?
func init() -> void:
	$RhythmTypeOptionButton.select(standard_view.data["Style"])
	$BPMTextEdit.text = str(int(standard_view.data["BPM"]))

# applies the UI data to the standard view
func apply_to_standard() -> void:
	standard_view.data["Style"] = $RhythmTypeOptionButton.selected
	
	var bpm_text: String = $BPMTextEdit.text
	if bpm_text.is_valid_int():
		standard_view.data["BPM"] = int(bpm_text)
		$BPMTextEdit.flat = false
	else:
		$BPMTextEdit.flat = true

# navigates to user://saves/(...) folder @ UserFolderButton pressed
func _on_user_folder_button_pressed() -> void:
	OS.shell_open(OS.get_user_data_dir() + "/saves/" + standard_view.standard_name)

# applies data @ BPMTextEdit text_changed
func _on_bpm_text_edit_text_changed(new_text: String) -> void:
	%UI.apply_all_to_standard()

# applies data @ RhythmTypeOptionButton item_selected
func _on_rhythm_type_option_button_item_selected(index: int) -> void:
	%UI.apply_all_to_standard()
