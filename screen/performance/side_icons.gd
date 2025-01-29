extends Node2D


# -
func _process(delta: float) -> void:
	var current_time: float = %MidiScheduler.CurrentTime
	
	for i in range(4):
		var icon: Sprite2D = get_child(i)
		
		icon.position.x = 224 + ((current_time / 4) - (2 * i + 1)) * 816
