extends Node2D


# -
func _process(delta: float) -> void:
	var current_time: float = %MidiScheduler.Time
	if current_time < 0: return
	current_time = fposmod(current_time, %MidiScheduler.SongLength)
	
	for i in range(4):
		var icon: Sprite2D = get_child(i)
		
		icon.position.x = 224 + ((current_time / 4) - (2 * i + 1)) * 832
		icon.position.x = clampf(icon.position.x, 224, 224 + 832)
