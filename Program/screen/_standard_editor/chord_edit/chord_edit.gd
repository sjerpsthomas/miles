extends Panel


@onready var key_option_button := $KeyOptionButton as OptionButton
@onready var type_option_button := $TypeOptionButton as OptionButton

var initialized := false

var key: StandardEditorMeasure.KeyEnum
var type: StandardEditorMeasure.TypeEnum


signal value_changed()
signal deleted()

func initialize(new_key: StandardEditorMeasure.KeyEnum, new_type: StandardEditorMeasure.TypeEnum) -> void:
	key = new_key
	type = new_type
	
	key_option_button.selected = key
	type_option_button.selected = type
	
	initialized = true

func _on_key_option_button_item_selected(index: int) -> void:
	if not initialized: return
	key = index as StandardEditorMeasure.KeyEnum
	value_changed.emit()

func _on_type_option_button_item_selected(index: int) -> void:
	if not initialized: return
	type = index as StandardEditorMeasure.TypeEnum
	value_changed.emit()

func _on_delete_button_pressed() -> void:
	deleted.emit()
