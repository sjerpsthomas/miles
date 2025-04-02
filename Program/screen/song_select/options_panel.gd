extends Panel


var current_standard: String


func _on_tab_bar_tab_changed(tab: int) -> void:
	$PerformancePanel.visible = tab == 0
	$RecordingPanel.visible = tab == 1


func _on_index_text_edit_text_changed(new_text: String) -> void:
	var params = new_text.split('/')
	var pupil = int(params[0])
	var session = int(params[1])
	var performance = int(params[2])
	
	var standard_index = PerformanceScreenInit.info["pupils"][pupil - 1]\
		["sessions"][session - 1]["performance"][performance - 1]["song"]
	
	current_standard = [
		"Long Ago and Far Away", "Summertime", "My Little Suede Shoes"
	][standard_index]
	
	%Standards.navigate_by_name(current_standard)
