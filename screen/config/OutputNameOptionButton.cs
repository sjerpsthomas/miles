using Godot;
using System;
using thesis.piano;
using static thesis.midi.MidiServer;

public partial class OutputNameOptionButton : OptionButton
{
	public Piano Piano;
	
	public override void _Ready()
	{
		foreach (var name in Enum.GetNames<OutputName>())
			AddItem(name);

		Piano = GetNode<Piano>("%Piano");
	}

	public void _on_item_selected(int selected)
	{
		Piano.OutputName = Enum.Parse<OutputName>(GetItemText(selected));
	}
}
