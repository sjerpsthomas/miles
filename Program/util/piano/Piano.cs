using System.Linq;
using Core.midi;
using Godot;
using Program.midi;

namespace Program.util.piano;

public partial class Piano : Node2D
{
	public OutputName OutputName = OutputName.Loopback;
	[Export] public bool ShowAll = false;
	
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
		var color = (OutputName)outputName switch
		{
			OutputName.Loopback => new Color("ffd400"),
			OutputName.Algorithm => new Color("6495ed"),
			_ when ShowAll => Colors.DeepPink,
			_ => new Color(0, 0, 0, 0)
		};

		if (color.A == 0) return;

		var index = note - 36;
		var key = GetChildren().FirstOrDefault(it => (int)it.Get("index") == index);
        
		key?.Call("update_pressed", velocity > 0, color);
	}
}