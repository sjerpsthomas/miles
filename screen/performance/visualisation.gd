extends Node2D


@onready var current_chord := $CurrentChord as ColorRect

@onready var standard_view := %StandardView as StandardView


func _process(delta: float) -> void:
	# get time, with lookahead
	var current_time: float = %MidiScheduler.CurrentTime
	current_time += 0.15
	
	if current_time < 0: return
	current_chord.visible = true
	
	var current_measure := floori(current_time)
	
	current_chord.position = standard_view.get_measure_position(current_measure) + standard_view.position
