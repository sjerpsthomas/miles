using System.Linq;
using Core.midi;
using Godot;
using Program.midi;

namespace Program.screen;

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

	public void _on_MidiServer_NoteSent(MidiNote note)
	{
		var index = note.Note % 12;
		
		if (note.Velocity > 0)
			Counts[index]++;
		else
		if (--Counts[index] < 0) Counts[index] = 0;

		if (IsInstanceValid(this))
			CallDeferred("DoPress");
	}
	
	public void DoPress() => EmitSignal(SignalName.Press);
}