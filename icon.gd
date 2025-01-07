extends Sprite2D


@onready var scene := get_parent()


# -
func _process(delta: float) -> void:
	var held_pattern: String = MidiPerformanceServer.GetHeldPattern()
	
	match held_pattern:
		"100001010000": position.x += 128 * delta
		"101000010000": position.x -= 128 * delta
		"100010010000": position.y += 128 * delta
		"100100010000": position.y -= 128 * delta
