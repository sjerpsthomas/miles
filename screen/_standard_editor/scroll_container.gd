extends ScrollContainer


var pos_x: float


func _ready() -> void:
	pos_x = position.x
	position.x = 252


func _process(delta: float) -> void:
	position.x = lerpf(position.x, pos_x, 0.15)
