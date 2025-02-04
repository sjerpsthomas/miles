extends ScrollContainer


# applies all UI data to the standard view
func apply_all_to_standard() -> void:
	# put all UI data into standard view
	%ChordPanel.apply_to_standard()
	%StandardPanel.apply_to_standard()
	
	# refresh standard view
	%StandardView.refresh()
