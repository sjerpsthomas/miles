extends Node2D

@export var key_offset := 60

@export var wrap_octave := false

@onready var scene := get_parent()

func _process(_delta: float) -> void:
	if wrap_octave:
		var held_pattern: String = get_parent().GetHeldPattern()
		
		for child: PianoKey in get_children():
			child.update_pressed(held_pattern[child.index % 12] == "1")
	else:
		for child: PianoKey in get_children():
			child.update_pressed(get_parent().Keys[child.index])
