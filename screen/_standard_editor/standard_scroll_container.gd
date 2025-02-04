extends ScrollContainer


var pos_x: float


# -
func _ready() -> void:
	# set start position
	pos_x = position.x
	position.x = 252

# -
func _process(delta: float) -> void:
	# interpolate to start position
	position.x = lerpf(position.x, pos_x, 0.15)
