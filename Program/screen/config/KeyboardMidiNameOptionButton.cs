using Godot;
using NAudio.Midi;

public partial class KeyboardMidiNameOptionButton : OptionButton
{
	public string KeyboardMidiName
	{
		get => (string)GetNode("/root/Config").Get("keyboard_midi_name");
		set => GetNode("/root/Config").Set("keyboard_midi_name", value);
	}
	
	public override void _Ready()
	{
		var currentKeyboardMidiName = KeyboardMidiName;
		
		// Add all MIDI in names
		for (var i = 0; i < MidiIn.NumberOfDevices; i++)
		{
			var name = MidiIn.DeviceInfo(i).ProductName;
			AddItem(name);

			// Select current keyboard_midi_name
			if (name == currentKeyboardMidiName)
				Select(i);
		}
	}

	public void _on_item_selected(int selected)
	{
		// Set keyboard_midi_name
		KeyboardMidiName = MidiIn.DeviceInfo(selected).ProductName;
		
		// Show restart label
		((Label)GetNode("%RestartLabel")).Visible = true;
	}
}
