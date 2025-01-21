using Godot;
using System;
using System.Linq;
using thesis.midi;
using thesis.midi.core;

public partial class InputScreen : Node
{
	public int[] Counts = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

	public string CountsString => string.Join(null, Counts.Select(it => it.ToString()));
	
	[Signal]
	public delegate void PressEventHandler();
    
	public override void _Ready()
	{
		MidiServer.Instance.NoteSent += _on_MidiServer_NoteSent;
	}

	public void _on_MidiServer_NoteSent(MidiServer.OutputName outputName, MidiNote note)
	{
		if (note.Velocity > 0)
			Counts[note.Note % 12]++;
		else
			Counts[note.Note % 12]--;

		if (IsInstanceValid(this))
			CallDeferred("DoPress");
	}
	
	public void DoPress() => EmitSignal(SignalName.Press);
}
