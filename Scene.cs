using Godot;
using Godot.Collections;
using thesis.midi;

namespace thesis;

using OutputName = MidiServer.OutputName;
using NoteData = midi.scheduler.MidiScheduler.NoteData;

public partial class Scene : Node2D
{
	public const int KeyCount = 61;
	
	public Array<bool> Keys;

	[Signal]
	public delegate void NoteReceivedEventHandler(int note, int velocity);
	
	public override void _Ready()
	{
		Keys = new Array<bool>();
		Keys.Resize(61);
		Keys.Fill(false);

		MidiServer.Instance.NoteSent += OnMidiServerNoteSent;
	}

	public void OnMidiServerNoteSent(OutputName outputName, NoteData noteData)
	{
		var note = noteData.Note;
		var pressed = noteData.Velocity > 0;

		// Remap from keyboard
		note -= 36;

		if (note is < 0 or > 60)
			return;
        
		Keys[note] = pressed;
	}

	public string GetHeldPattern()
	{
		var chars = new char[12];
		for (var i = 0; i < 12; i++)
			chars[i] = '0';
		
		for (var i = 0; i < KeyCount; i++)
			if (Keys[i]) chars[i % 12] = '1';

		return string.Concat(chars);
	}
}
