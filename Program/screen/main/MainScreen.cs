namespace Program.screen.main;

public partial class MainScreen : InputScreen
{
	public void _on_configuration_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://screen/config/config_screen.tscn");
	}
}