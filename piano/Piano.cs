using Godot;
using thesis.midi;

namespace thesis.piano;

public partial class Piano : Node2D
{
	[Export] public MidiServer.OutputName OutputName;
		
	public override void _Ready()
	{
		MidiServer.Instance.NoteSent += note =>
			CallDeferred("_on_MidiServer_NoteSent", (int)note.OutputName, note.Note, note.Velocity);
	}
	
	public void _on_MidiServer_NoteSent(int outputName, int note, int velocity)
	{
		if ((MidiServer.OutputName)outputName != OutputName) return;

		var index = note - 36;
		var key = GetChild(index);
        
		key.Call("update_pressed", velocity > 0);
	}
}