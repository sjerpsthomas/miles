using System.Linq;
using Core.midi;
using Godot;
using Godot.Collections;
using Program.midi;

namespace Program.util;

public partial class Piano : Node2D
{
	[Export] public Array<OutputName> OutputNames;

	[Export] public Color PressedColor = Colors.DeepPink;
	
	public override void _Ready()
	{
		MidiServer.Instance.DeferredNoteSent += _on_MidiServer_NoteSent;
	}
	
	public override void _ExitTree()
	{
		MidiServer.Instance.DeferredNoteSent -= _on_MidiServer_NoteSent;
	}
	
	public void _on_MidiServer_NoteSent(int outputName, int note, int velocity)
	{
		if (!OutputNames.Contains((OutputName)outputName)) return;

		var index = note - 36;
		var key = GetChildren().FirstOrDefault(it => (int)it.Get("index") == index);
        
		key?.Call("update_pressed", velocity > 0);
	}
}