extends Node2D


@onready var current_chord := $CurrentChord as ColorRect


func _process(delta: float) -> void:
	var current_time: float = %MidiScheduler.CurrentTime
	if current_time < 0: return
	
	var current_measure := floori(current_time)
	
	current_chord.position = Vector2(
		256 + (current_measure % 4) * 192,
		16 + (current_measure / 4 + 1) * 58
	)
