class_name Piano
extends Node2D

@export var key_offset := 60

@export var wrap_octave := false

@onready var scene := get_parent()

func _process(delta: float) -> void:
	if wrap_octave:
		var held_pattern: String = MidiPerformanceServer.GetHeldPattern()
		
		for child: PianoKey in get_children():
			child.update_pressed(held_pattern[child.index % 12] == "1")
	else:
		for child: PianoKey in get_children():
			child.update_pressed(MidiPerformanceServer.Keys[child.index])
