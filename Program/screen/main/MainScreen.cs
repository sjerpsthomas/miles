using Core;
using Godot;

namespace thesis.screen.main;

public partial class MainScreen : InputScreen
{
	public override void _Ready()
	{
		var thing = new Thing();
		GD.Print(thing.Do(3));
		base._Ready();
	}

	public void _on_configuration_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://screen/config/config_screen.tscn");
	}
}