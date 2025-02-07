using System;
using Core.midi;
using Godot;
using Piano = Program.util.Piano;

namespace Program.screen.config;

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
		Piano.OutputNames = [Enum.Parse<OutputName>(GetItemText(selected))];
	}
}