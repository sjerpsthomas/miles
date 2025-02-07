using Core.midi;
using Godot;
using Godot.Collections;
using Program.midi;
using Program.midi.scheduler;

namespace Program.screen.performance;

public partial class PerformanceScreen : Node2D
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

		var standardName = (string)GetNode("/root/PerformanceScreenInit").Get("standard_name");
		GetNode("StandardView").Call("load_sheet", standardName);
	}

	public void OnMidiServerNoteSent(MidiNote noteData)
	{
		if (noteData.OutputName is not (OutputName.Loopback or OutputName.Algorithm)) return;
		
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

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel")) Quit();
	}

	public void Quit()
	{
		// Stop scheduler
		GetNode<MidiScheduler>("%MidiScheduler").Stop();
		
		// Change scene
		GetTree().ChangeSceneToFile("res://screen/song_select/song_select_screen.tscn");
	}
}
