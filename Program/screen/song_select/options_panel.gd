extends Panel


var current_standard: String


func _ready() -> void:
	# Get the previously-played notes path (not very elegant)
	var notes_path := PerformanceScreenInit.notes_path
	if notes_path == "": notes_path = "1/1/1"
	
	%TabBar.current_tab = 1
	_on_tab_bar_tab_changed(1)
	
	notes_path = notes_path.replace("res://recordings/", "")
	notes_path = notes_path.replace(".notes", "")
	
	%IndexTextEdit.text = notes_path
	update_current_standard(notes_path)


func update_current_standard(text: String) -> void:
	%IndexTextEdit.flat = true
	
	var params = text.split('/')
	if params.size() != 3: return
	
	var pupil = int(params[0])
	if pupil < 1 or pupil > 5: return
	
	var session = int(params[1])
	if session < 1 or session > 4: return
	
	var performance = int(params[2])
	if performance < 1 or performance > 3: return
	
	var standard_index = PerformanceScreenInit.info["pupils"][pupil - 1]\
		["sessions"][session - 1]["performance"][performance - 1]["song"]
	
	current_standard = [
		"Long Ago and Far Away", "Summertime", "My Little Suede Shoes"
	][standard_index]
	
	%IndexTextEdit.flat = false
	%Standards.navigate_by_name(current_standard)

func _on_tab_bar_tab_changed(tab: int) -> void:
	$PerformancePanel.visible = tab == 0
	$RecordingPanel.visible = tab == 1

func _on_index_text_edit_text_changed(new_text: String) -> void:
	update_current_standard(new_text)
