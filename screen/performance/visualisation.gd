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
	
	var is_human_playing := current_measure % 8 < 4
	
	current_chord.position = standard_view.get_measure_position(current_measure) + standard_view.position
	current_chord.color = Color("ffd40080") if is_human_playing else Color("6495ed80")
	
	%Piano.PressedColor = Color("ffd400") if is_human_playing else Color("6495ed")
	
	var target_color := Color.WHITE if is_human_playing else Color.CORNFLOWER_BLUE
	%Background.color = (%Background.color as Color).lerp(target_color, 0.15)
