extends Node


var info: Dictionary

func _ready() -> void:
	var file := FileAccess.open("res://recordings/info.json", FileAccess.READ)
	var text := file.get_as_text()
	info = JSON.parse_string(text)

var standard_name: String

var soloist: int
var repetition_count: int

var is_pupil: bool

var pupil_info: String

var notes_path: String
var start_measure: int
