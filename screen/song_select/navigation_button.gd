extends "res://screen/chord_button.gd"


@export var direction: int


func _pressed() -> void:
	$"../Standards".navigate(direction)
