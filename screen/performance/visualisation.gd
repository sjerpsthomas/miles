extends Node2D


@onready var current_chord := $CurrentChord as ColorRect


func _process(delta: float) -> void:
	# get time, with lookahead
	var current_time: float = %MidiScheduler.CurrentTime
	current_time += 0.1
	
	if current_time < 0: return
	current_chord.visible = true
	
	var current_measure := floori(current_time)
	
	current_chord.position = Vector2(
		256 + (current_measure % 4) * 192,
		(current_measure / 4 + 1) * 60
	)
