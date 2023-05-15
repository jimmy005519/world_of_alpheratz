namespace Alpheratz;

public partial class TitleScene : Control
{
	[Export]
	PlatformerPlayer player;
	[Export]
	GameMenu menu;
	[Export]
	Control logo;

	public override void _Ready()
	{
		player.scriptedInput = true;
		player.x_input = 1.0f;
		player.PlayerStateChanged += (PlayerState oldState, PlayerState newState) => {
			if (oldState == PlayerState.Falling)
			{
				menu.ShowMenu(menu.topMenu);
				CreateTween().TweenProperty(logo, "modulate:a", 1.0f, 0.5f);

				// User can override the default bgm by placing a file at user://bgm.mp3

				string bgmPath = "res://sounds/bgm_Grocery Store_ Calm Shop Game Music by HeatleyBros.mp3";

				if (FileAccess.FileExists("user://bgm.mp3"))
				{
					bgmPath = "user://bgm.mp3";
				}

				GameWorld.Instance.PlayBGM(bgmPath);
			}
		};
		logo.Modulate = logo.Modulate with {A = 0};


		CreateTween().SetLoops().TweenCallback(Callable.From(() => {
			if (GD.Randf() > 0.5f)
			{
				player.emoBubble.PlayEmo(3);
			}
		})).SetDelay(5.0f);

	}

}
