class_name PianoKey
extends ColorRect


@onready var index := get_index()

var entered := false
var clicked := false


func _ready() -> void:
	mouse_entered.connect(_on_mouse_entered)
	mouse_exited.connect(_on_mouse_exited)


func _process(_delta: float) -> void:
	var new_clicked := Input.is_action_pressed("click") and entered
	
	if not clicked and new_clicked:
		MidiServer.Send(0, index + 36, 100)
	elif clicked and not new_clicked:
		MidiServer.Send(0, index + 36, 0)
	
	clicked = new_clicked

func update_pressed(pressed: bool) -> void:
	modulate = Color.GRAY if pressed else Color.WHITE

func _on_mouse_entered() -> void: entered = true

func _on_mouse_exited() -> void: entered = false
