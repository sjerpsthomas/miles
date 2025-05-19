extends Panel


var current_standard: String


func _ready() -> void:
	# Get the previously-played notes path (not very elegant)
	var notes_path := PerformanceScreenInit.notes_path
	if notes_path == "": notes_path = "ex2_par0_per0"
	
	# TODO: may be commented out
	%TabBar.current_tab = 1
	_on_tab_bar_tab_changed(1)


func update_current_standard(text: String) -> void:
	%IndexTextEdit.flat = true
	
	if not (text in PerformanceScreenInit.info): return
	var performance_info = PerformanceScreenInit.info[text]
	
	current_standard = [
		"Long Ago and Far Away", "Summertime", "My Little Suede Shoes", "Billies Bounce"
	][performance_info["song"]]
	
	%IndexTextEdit.flat = false
	%Standards.navigate_by_name(current_standard)

func _on_tab_bar_tab_changed(tab: int) -> void:
	$PerformancePanel.visible = tab == 0
	$RecordingPanel.visible = tab == 1

func _on_index_text_edit_text_changed(new_text: String) -> void:
	update_current_standard(new_text)
