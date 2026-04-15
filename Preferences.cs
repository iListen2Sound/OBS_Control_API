using MelonLoader;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using System.Collections;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Managers;
using UnityEngine.Rendering;
using System.Security.AccessControl;
using System.Threading;
using System.Reflection;
using UIFramework;
using System.IO;

namespace OBS_Control_API
{
	/** 
	 * <summary>
	 * Class responsible for Defining MelonPreferences
	 * </summary>
	 */
	public class Preferences
	{
		

		internal static MelonPreferences_Category GeneralCategory;
		
		internal static MelonPreferences_Entry<bool> EnableReplayBuffer;
		internal static MelonPreferences_Entry<float> ReplayBufferBuffer;
		internal static MelonPreferences_Entry<ControllerKeyActions> BindingLeft;
		internal static MelonPreferences_Entry<ControllerKeyActions> BindingRight;
		internal static MelonPreferences_Entry<float> HapticDuration;
		internal static MelonPreferences_Entry<bool> AudioFeedback;


		internal static MelonPreferences_Category Connection;

		internal static MelonPreferences_Entry<string> IpAddress;
		internal static MelonPreferences_Entry<int> Port;
		internal static MelonPreferences_Entry<string> Password;
		/**
		 * <summary>
		 * Initializes the MelonPreferences entries and creates the config file if it doesn't exist.
		 * </summary>
		 */
		public static void PrefInit()
		{
			if (!Directory.Exists(OBS.USER_DATA))
				Directory.CreateDirectory(OBS.USER_DATA);


			GeneralCategory = MelonPreferences.CreateCategory("OBS_Control_API", "General Settings");
			GeneralCategory.SetFilePath(Path.Combine(OBS.USER_DATA, OBS.CONFIG_FILE));

			EnableReplayBuffer = GeneralCategory.CreateEntry("EnableReplayBuffer", true, "Force Enable Replay Buffer", "Never forget to start the replay buffer again!\n" +
				"The mod will start it for you on connection and stop it as you close the game.");
			ReplayBufferBuffer = GeneralCategory.CreateEntry("ReplayBufferBuffer", 0f, "Replay Save Delay", "Delay saving a replay buffer after pressing the keybind so your clips don't end suddenly");

			BindingLeft = GeneralCategory.CreateEntry("BindingLeft", ControllerKeyActions.SaveReplayBuffer, "Left Controller Binding", "Action to perform when both buttons on the left controller are being pressed.");
			BindingRight = GeneralCategory.CreateEntry("BindingRight", ControllerKeyActions.Nothing, "Right Controller Binding", "Action to perform when both buttons on the right controller are being pressed.");
			HapticDuration = GeneralCategory.CreateEntry("HapticDuration", 0.2f, "Haptic Feedback Duration", "Duration of the haptic impulse when an action is successful (set to 0 to disable).");
			AudioFeedback = GeneralCategory.CreateEntry("AudioFeedback", true, "Audio Feedback", "Set to true to get a sound effect when an action is successful.");

			Connection = MelonPreferences.CreateCategory("WebsocketConnection", "OBS Connection");
			Connection.SetFilePath(Path.Combine(OBS.USER_DATA, OBS.CONFIG_FILE));

			IpAddress = Connection.CreateEntry("IpAddress", "localhost", "IP Address", "IP address of the OBS websocket server.");
			Port = Connection.CreateEntry("Port", 4455, "Port", "Port used by the OBS websocket server.");
			Password = Connection.CreateEntry("Password", "", "Websocket Password", "Password for the OBS websocket server.");

		}

	}
	/**
	 * <summary>
	 * Enum defining the possible actions that can be bound to the controller buttons.
	 * </summary>
	 */
	internal enum ControllerKeyActions
	{
		Nothing,
		SaveReplayBuffer,
		StartRecording,
		StopRecording,
		ToggleRecording,
		SaveScreenshot,
	}
}