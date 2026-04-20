using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using ClipperLib;
using Il2CppRUMBLE.Audio;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Players.Subsystems;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using UIFramework;
using UnityEngine;
using UnityEngine.Rendering;
using static Il2CppRUMBLE.Audio.AudioCall;
using static RumbleModdingAPI.RMAPI.AudioManager;
using static RumbleModdingAPI.RMAPI.GameObjects.Gym.TUTORIAL.Worldtutorials.CombatCarvings.CombatCarvingCombos.CarvingHeadParent.CombosCarvinghead.Players;
using static UnityEngine.Rendering.ProbeReferenceVolume;

namespace OBS_Control_API
{
	public static class BuildInfo
	{
		public const string ModName = "OBS_Control_API";
		public const string ModVersion = "1.3.0";
		public const string Description = "Manages a websocket connection to OBS";
		public const string Author = "Kalamart";
		public const string Company = "";
	}
	public partial class OBS : MelonMod
	{
        internal const string USER_DATA = "UserData/OBS_Control_API/";
        internal const string AUDIO_RESOURCES = "sfx_assets/";
        internal const string CONFIG_FILE = "config.cfg";

		//constants
		private bool forceReplayBuffer = true;
		private float replayBufferBuffer = 0;
		private static string ip = "localhost";
		private static int port = 4455;
		private static string password = "your_password_here";
		private ControllerKeyActions[] keyBindings = { ControllerKeyActions.Nothing, ControllerKeyActions.Nothing };
		private bool[] bindingLocked = { false, false };
		private float hapticsDuration = 1;
		private bool enableSFX = true;
        private float SFXvolume = 1;

        //AudioCalls
        private static AudioCall Confirmation;
		private static AudioCall Screenshot;
		private static AudioCall StartRecording;
		private static AudioCall StopRecording;


		// variables
		private static ConnectionManager connectionManager = null;
		private static RequestManager requestManager = null;
		private static PlayerHaptics playerHaptics = null;

		private static bool isReplayBufferActive = false;
		private static bool isRecordingActive = false;
		private static bool isStreamActive = false;
		private static bool isWindows = true;
		private static string recordingDirectory = "";
		private static string sceneUuid = "";
		private static bool stopReplayBufferAtShutdown = false;
        private static bool areHapticsPlaying = false;

        private static GameObject OBS_SFX_Players = null;
		private static GameObject screenshotSFXPlayer = null;
		private static GameObject confirmationSFXPlayer = null;
		private static GameObject startRecordingSFXPlayer = null;
		private static GameObject stopRecordingSFXPlayer = null;

		/**
         * <summary>
         * Log to console.
         * </summary>
         */
		private static void Log(string msg)
		{
			MelonLogger.Msg(msg);
		}
		/**
         * <summary>
         * Log to console but in yellow.
         * </summary>
         */
		private static void LogWarn(string msg)
		{
			MelonLogger.Warning(msg);
		}
		/**
         * <summary>
         * Log to console but in red.
         * </summary>
         */
		private static void LogError(string msg)
		{
			MelonLogger.Error(msg);
		}

		/**
         * <summary>
         * Initialize the websocket client.
         * </summary>
         */
		private void InitClient()
		{
			requestManager = new RequestManager();
			connectionManager = new ConnectionManager(requestManager, ip, port, password);
			InitEvents();
			onConnect += OnConnect;
			onDisconnect += OnDisconnect;
		}
		/**
		 * <summary>
		 * Create audio calls for sound effects through RMAPI
		 * </summary>
		 * <remarks>
		 * New system by both Ulvak and TacoSlayer. Might be helpful to ask those two for help
		 * -iListen2Sound
		 * </remarks>
		 */
		private void InitAudioCalls()
		{
			try
			{
				Confirmation = CreateAudioCall(Path.Combine(USER_DATA, AUDIO_RESOURCES, "confirmation.wav"), SFXvolume);
				Screenshot = CreateAudioCall(Path.Combine(USER_DATA, AUDIO_RESOURCES, "screenshot.wav"), SFXvolume);
				StartRecording = CreateAudioCall(Path.Combine(USER_DATA, AUDIO_RESOURCES, "start_recording.wav"), SFXvolume);
				StopRecording = CreateAudioCall(Path.Combine(USER_DATA, AUDIO_RESOURCES, "stop_recording.wav"), SFXvolume);
            }
			catch (Exception e)
			{
				LogError($"Error initializing audio calls: {e}");
				enableSFX = false;
			}
		}
		/**
		 * <summary>
		 * Called when the mod is initialized, before any scene is loaded.
		 * Initializes the MelonPreferences and registers the onsave event handler.
		 * Calls PopulateUserDataIfNeeded to extract the embedded sound effect assets
		 * to the user data folder, so they can be loaded by Unity's audio system.
		 * </summary>
		 */
		public override void OnInitializeMelon()
		{
			Preferences.PrefInit();
			if (Preferences.Password.Value == "")
            {
                UI.Register((MelonBase)this, Preferences.Connection, Preferences.GeneralCategory).OnModSaved += OnUISaved;
            }
			else
			{
                UI.Register((MelonBase)this, Preferences.GeneralCategory, Preferences.Connection).OnModSaved += OnUISaved;
            }

            PopulateUserDataIfNeeded();
			InitAudioCalls();
		}


		/**
         * <summary>
         * Called when the scene has finished loading.
         * When we are in the loader scene, starts the connection thread.
         * </summary>
         */
		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			playerHaptics = PlayerManager.instance.playerControllerPrefab.gameObject.GetComponent<PlayerHaptics>();
			if (sceneName == "Loader")
			{
				InitClient();
				OnUISaved();
			}
		}

		/**
         * <summary>
         * (Re)start connection thread.
         * </summary>
         */
		public static void Connect()
        {
            connectionManager.UpdateWebsocketConfig(ip, port, password);
            if (IsConnected())
			{
				Disconnect(false);
			}
			else
			{
                connectionManager.Start();
            }
		}

		/**
         * <summary>
         * Close the connection and don't try to reconnect.
         * </summary>
         */
		public static void Disconnect(bool stopReconnect = true)
		{
			connectionManager.Stop(stopReconnect);
		}

		/**
         * <summary>
         * Called when the connection with OBS is 100% established and authentication is complete.
         * </summary>
         */
		private void OnConnect()
		{
			SetMainStatus();
			if (!isReplayBufferActive && forceReplayBuffer)
			{
				Log($"Replay Buffer was inactive, starting it...");
				stopReplayBufferAtShutdown = true;
				StartReplayBuffer();
			}
		}

		/**
         * <summary>
         * Called when the connection with OBS is closed.
         * </summary>
         */
		private void OnDisconnect()
		{
			isReplayBufferActive = false;
			isRecordingActive = false;
			isStreamActive = false;
		}

		/**
         * <summary>
         * Called from the save button in UI Framework while this mod is selected
         * </summary>
         */
		private void OnUISaved()
        {
            forceReplayBuffer = Preferences.EnableReplayBuffer.Value;
			replayBufferBuffer = Preferences.ReplayBufferDelay.Value;
			keyBindings[0] = Preferences.BindingLeft.Value;
			keyBindings[1] = Preferences.BindingRight.Value;
			hapticsDuration = Preferences.HapticDuration.Value;
			enableSFX = Preferences.AudioFeedback.Value;
            SFXvolume = Preferences.AudioVolume.Value;

            ip = Preferences.IpAddress.Value;
			port = Preferences.Port.Value;
			password = Preferences.Password.Value;

			UpdateSFXvolume();

            Connect();
		}

        /**
         * <summary>
         * Updates the volume of all audio calls to match the value in the config.
         * </summary>
         */
        private void UpdateSFXvolume()
        {
            AudioCall.GeneralAudioSettings generalAudioSettings = new AudioCall.GeneralAudioSettings();
            generalAudioSettings.SetVolume(SFXvolume);
            generalAudioSettings.Pitch = 1f;
            Confirmation.generalSettings = generalAudioSettings;
            Screenshot.generalSettings = generalAudioSettings;
            StartRecording.generalSettings = generalAudioSettings;
            StopRecording.generalSettings = generalAudioSettings;
        }

        /**
         * <summary>
         * Executes an action that is connected to a key binding.
         * </summary>
         */
        private void ExecuteKeyBinding(int index, Vector3 audioLocation)
		{
			bindingLocked[index] = true;
			bool success = false;
			AudioCall audioPlayer = null;
			switch (keyBindings[index])
			{
				case ControllerKeyActions.SaveReplayBuffer:
                    if (hapticsDuration > 0)
                    {
                        // first haptic impulse to show that the request is being processed
                        HapticFeedback(hapticsDuration);
                    }
                    MelonCoroutines.Start(DelayReplayBufferSaving(Confirmation, audioLocation));
					success = false; // prevent double feedback
					break;
				case ControllerKeyActions.StartRecording:
					if (StartRecord())
					{
						Log($"Started recording");
						audioPlayer = StartRecording;
						success = true;
					}
					break;
				case ControllerKeyActions.StopRecording:
					{
						var res = StopRecord();
						if (res != null)
						{
							Log($"Stopped recording, saved to: {res.outputPath}");
							audioPlayer = StopRecording;
							success = true;
						}
					}
					break;
				case ControllerKeyActions.ToggleRecording:
					{
						var res = ToggleRecord();
						if (res != null)
						{
							var active = res.outputActive;
							string toggleValue = active ? "Started" : "Stopped";
							Log($"{toggleValue} recording");
							audioPlayer = active ? StartRecording : StopRecording;
							success = true;
						}
					}
					break;
				case ControllerKeyActions.SaveScreenshot:
					if (SaveSourceScreenshot())
					{
						Log($"Saved screenshot");
						audioPlayer = Screenshot;
						success = true;
					}
					break;
				default:
					Log($"Invalid key binding \"{keyBindings[index]}\"");
					break;
			}

			if (success)
			{
				Feedback(audioPlayer, audioLocation);
			}
			MelonCoroutines.Start(UnlockKeyBinding(index));
		}

		/**
         * <summary>
         * Waits for 1 second before making the key binding available again
         * </summary>
         */
		private IEnumerator UnlockKeyBinding(int index)
		{
			yield return new WaitForSeconds(1f);
			bindingLocked[index] = false;
			yield break;
		}

		/**
		 * <summary>
		 * Waits for the amount of time specified in the config as "ReplayBufferDelay"
		 * before saving the replay buffer to avoid having clips that end too suddenly.
		 * </summary>
		 */
		private IEnumerator DelayReplayBufferSaving(AudioCall audioPlayer, Vector3 audioLocation)
		{
			yield return new WaitForSeconds(replayBufferBuffer);
			if (SaveReplayBuffer())
			{
				Log($"Saved replay buffer");
                Feedback(audioPlayer, audioLocation);
            }
		}


		/**
         * <summary>
         * If needed, perform a feedback to notify the user of an event.
         * </summary>
         */
		private void Feedback(AudioCall audioPlayer, Vector3 AudioLocation)
		{
			if (enableSFX && audioPlayer is not null)
			{
				RumbleModdingAPI.RMAPI.AudioManager.PlaySound(audioPlayer, AudioLocation);
			}
			if (hapticsDuration > 0)
			{
				HapticFeedback(hapticsDuration);
			}
		}

		/**
         * <summary>
         * Play the confirmation tone that is used for the "save replay buffer" action.
         * </summary>
         */
		public static void playConfirmationSFX()
		{
			confirmationSFXPlayer.GetComponent<AudioSource>().Play();
		}

		/**
		 * <summary>
		 * Checks the userdata folder if sound effect assets are present. Extract and write them if needed
		 * </summary>
		 */
		static void PopulateUserDataIfNeeded()
		{
			string folderName = "sfx_assets";
			string effectSoundsDir = USER_DATA + $"/{folderName}/";

            if (!Directory.Exists(effectSoundsDir))
            {
                Directory.CreateDirectory(effectSoundsDir);
            }

            // if the directory is not empty, that means there are audio files that the user
            // might have replaced with their own, so we shouldn't overwrite them.
            if (Directory.GetFiles(effectSoundsDir).Length.Equals(0))
			{
				var assembly = typeof(OBS).Assembly;
				var resourceNames = assembly.GetManifestResourceNames()
					.Where(r => r.StartsWith($"OBS_Control_API.Resources.{folderName}.", StringComparison.OrdinalIgnoreCase));

				foreach (var resourceName in resourceNames)
				{
					string fileName = resourceName.Substring($"OBS_Control_API.Resources.{folderName}.".Length);
					string outPath = Path.Combine(effectSoundsDir, fileName);

					using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
					using (var fileStream = File.Create(outPath))
					{
						resourceStream.CopyTo(fileStream);
					}
				}
			}
		}

		/**
         * <summary>
         * Perform a haptic impulse on both controllers.
         * </summary>
         */
		public static void HapticFeedback(float duration)
		{
			if (playerHaptics != null)
			{
				areHapticsPlaying = true;
                MelonCoroutines.Start(StopHapticFeedback(duration));
            }
        }

        /**
         * <summary>
         * Stop the haptic feedback loop.
         * </summary>
         */
        public static IEnumerator StopHapticFeedback(float duration)
        {
            yield return new WaitForSeconds(duration);
            areHapticsPlaying = false;
        }

		/**
		 * <summary>
		 * Called on EVERY frame, used for VERY frequent updates only.
		 * </summary>
		 */
		public override void OnUpdate()
        {
            if (playerHaptics != null && areHapticsPlaying)
            {
                playerHaptics.PlayControllerHaptics(1, 1, 1, 1);
            }
        }

        /**
         * <summary>
         * Called 50 times per second, used for frequent updates.
         * </summary>
         * <remarks>
         * Because the new audio call system requires a location source,
         * I opted to use the local player's UI as the source.
         * Defaults to centerpoint of the map if that errors
         * -iListen2Sound
         * </remarks>
         */
        public override void OnFixedUpdate()
		{
			if (!bindingLocked[0] && keyBindings[0] != ControllerKeyActions.Nothing)
			{
				if (Calls.ControllerMap.LeftController.GetPrimary() > 0 && Calls.ControllerMap.LeftController.GetSecondary() > 0)
				{
					Log($"Left key binding activated");
					//ExecuteKeyBinding(0);
					new Thread(() =>
					{
						ExecuteKeyBinding(0, GetPlayerUILocation());
					}).Start();
				}
			}
			if (!bindingLocked[1] && keyBindings[1] != ControllerKeyActions.Nothing)
			{
				if (Calls.ControllerMap.RightController.GetPrimary() > 0 && Calls.ControllerMap.RightController.GetSecondary() > 0)
				{
					Vector3 audioPoint = Vector3.zero;
					try
					{
						audioPoint = PlayerManager.Instance.LocalPlayer.Controller.gameObject.transform.GetChild(4).GetChild(0).position;
					}
					catch (Exception ex) { }
					Log($"Right key binding activated");
					//ExecuteKeyBinding(1);
					new Thread(() =>
					{
						ExecuteKeyBinding(1, GetPlayerUILocation());
					}).Start();
				}
            }
        }
		/**
		 * <summary>
		 * Get the position of the player's UI in the world to use as a location for the audio source. 
		 * </summary>
		 */

		private Vector3 GetPlayerUILocation()
		{
			Vector3 audioPoint = Vector3.zero;
			try
			{
				audioPoint = PlayerManager.Instance.LocalPlayer.Controller.gameObject.transform.GetChild(4).GetChild(0).position;
			}
			catch (Exception ex) { }
			return audioPoint;
		}

		/**
         * <summary>
         * Called when the game is closed cleanly (by closing the window).
         * </summary>
         */
		public override void OnApplicationQuit()
		{
			if (connectionManager.IsConnected() && isReplayBufferActive && stopReplayBufferAtShutdown)
			{
				Log("Stopping replay buffer");
				StopReplayBuffer();
			}
			Disconnect(false);
		}

		/**
         * <summary>
         * Returns true if the client is connected to OBS, ready to send requests and receive events.
         * </summary>
         */
		public static bool IsConnected()
		{
			return connectionManager.IsConnected();
		}

		/**
         * <summary>
         * Returns true if the replay buffer is active.
         * </summary>
         */
		public static bool IsReplayBufferActive()
		{
			return isReplayBufferActive;
		}

		/**
         * <summary>
         * Returns true if recording is active.
         * </summary>
         */
		public static bool IsRecordingActive()
		{
			return isRecordingActive;
		}

		/**
         * <summary>
         * Returns true if streaming is active.
         * </summary>
         */
		public static bool IsStreamActive()
		{
			return isStreamActive;
		}
	}
}
