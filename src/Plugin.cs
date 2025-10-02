using BepInEx;
using BepInEx.Logging;
using RWCustom;
using System;
using System.Security.Permissions;
using UnityEngine;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace MiceColor;

[BepInPlugin("magica.mousecolor", "RWSC Offering Color Picker", "0.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;
    bool IsInit;
	private ColorChecker colorChecker;

	public void OnEnable()
    {
        Logger = base.Logger;
        On.RainWorld.OnModsInit += OnModsInit;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        if (IsInit) return;
        IsInit = true;

		try
		{
			ApplyHooks();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
    }

	private void ApplyHooks()
	{
		On.Player.ReleaseGrasp += Player_ReleaseGrasp;
		On.Player.ThrowObject += Player_ThrowObject;
		On.Player.SlugcatGrab += Player_SlugcatGrab;
		On.RainWorldGame.ExitGame += RainWorldGame_ExitGame;
	}

	private void Player_ReleaseGrasp(On.Player.orig_ReleaseGrasp orig, Player self, int grasp)
	{
		if (self.grasps[grasp] != null && self.grasps[grasp].grabbed is LanternMouse)
		{
			colorChecker?.Destroy();
			colorChecker = null;
		}

		orig(self, grasp);
	}

	private void RainWorldGame_ExitGame(On.RainWorldGame.orig_ExitGame orig, RainWorldGame self, bool asDeath, bool asQuit)
	{
		colorChecker?.Destroy();
		colorChecker = null;

		orig(self, asDeath, asQuit);
	}

	private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
	{
		if (self.grasps[grasp] != null && self.grasps[grasp].grabbed is LanternMouse)
		{
			colorChecker?.Destroy();
			colorChecker = null;
		}

		orig(self, grasp, eu);
	}

	private void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
	{
		orig(self, obj, graspUsed);

		if (obj is LanternMouse mouse)
		{
			colorChecker ??= new(mouse);
		}
	}


}
internal class ColorChecker
{
	private LanternMouse mouse;
	private FSprite mainSprite;
	private FSprite validSprite;
	private FLabel idString;

	private Vector2 screenSize
	{
		get
		{
			return Custom.rainWorld.options.ScreenSize;
		}
	}

	public ColorChecker(LanternMouse mouse)
	{
		this.mouse = mouse;

		// Our lantern mouse's direct light color
		mainSprite = new FSprite("pixel")
		{
			scale = 50f,
			anchorX = 0f,
			anchorY = 1f,
			color = Custom.HSL2RGB(mouse.iVars.color.hue, mouse.iVars.color.saturation, mouse.iVars.color.lightness),
		};
		Futile.stage.AddChild(mainSprite);
		mainSprite.SetPosition(20f, screenSize.y - 20f);

		validSprite = new FSprite(ColorWithinValidRange() ? "karmaRing" : "karma9-9")
		{
			color = Color.black,
			alpha = 0.8f,
			scale = 0.5f
		};
		Futile.stage.AddChild(validSprite);
		validSprite.SetPosition(mainSprite.GetPosition() + new Vector2(mainSprite.width / 2f, -(mainSprite.height / 2f)));

		idString = new(Custom.GetFont(), $"ID.1.{mouse.abstractCreature.ID.number}")
		{
			anchorX = 0f,
			anchorY = 0f,
		};
		Futile.stage.AddChild(idString);
		idString.SetPosition(mainSprite.GetPosition() - new Vector2(0f, 20f + mainSprite.height));
	}

	/* LANTERN MOUSE COLOR ASSIGNMENT:
	if (UnityEngine.Random.value < 0.01f)
	{
		hue = UnityEngine.Random.value;
	}
	else if (UnityEngine.Random.value < 0.5f)
	{
		hue = Mathf.Lerp(0f, 0.1f, UnityEngine.Random.value);
	}
	else
	{
		hue = Mathf.Lerp(0.5f, 0.65f, UnityEngine.Random.value);
	}

	So if Random.value hits the 1% chance, the Lantern Mouse will be a random color. 
	To qualify as valid for the challenge, the Lantern Mouse will need to fall into one of the hues not explicitly rolled for.
	So the check excludes these ranges: 0 - 0.1, 0.5 - 0.65.
	These ranges act as the common red and blue variants.
	 */
	private bool ColorWithinValidRange()
	{
		return mouse != null && mouse.iVars.color.hue > 0.1f && (mouse.iVars.color.hue < 0.5f || mouse.iVars.color.hue > 0.65f);
	}

	internal void Destroy()
	{
		mainSprite?.RemoveFromContainer();
		mainSprite = null;

		validSprite?.RemoveFromContainer();
		validSprite = null;

		idString?.RemoveFromContainer();
		idString = null;
	}
}
