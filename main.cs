using MelonLoader;
using RumbleModUI;
using RumbleModdingAPI;
using UnityEngine;
using System.Collections;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Managers;
using UnityEngine.Rendering;
using System.Security.AccessControl;
using System.Threading;

namespace OBS_Control_API
{
    public static class BuildInfo
    {
        public const string ModName = "OBS_Control_API";
        public const string ModVersion = "1.2.0";
        public const string Description = "Manages a websocket connection to OBS";
        public const string Author = "Kalamart";
        public const string Company = "";
    }
    public partial class OBS : MelonMod
    {
        //constants
        private bool forceReplayBuffer = true;
        private static string ip = "localhost";
        private static int port = 4455;
        private static string password = "your_password_here";
        private string[] keyBindings = { "Nothing", "Nothing" };
        private bool[] bindingLocked = { false, false };
        private float hapticsDuration = 1;
        private bool enableSFX = true;

        // variables
        Mod Mod = new Mod();
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

        //private static Il2CppAssetBundle bundle;

        private GameObject createAudioPlayer(AudioClip clip, string objectName, float volume)
        {
            GameObject player = new GameObject(objectName);
            player.transform.SetParent(OBS_SFX_Players.transform);
            AudioSource audioSource = player.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
            return player;
        }
        private void LoadAssets()
        {
            string bundleName = "OBS_Control_API.Resources.obs_sfx";

            Log($"Reading assets in bundle {bundleName}");
            var screenshotSFX = Calls.LoadAssetFromStream<AudioClip>(this, bundleName, "screenshot");
            var confirmationSFX = Calls.LoadAssetFromStream<AudioClip>(this, bundleName, "confirmation");
            var startRecordingSFX = Calls.LoadAssetFromStream<AudioClip>(this, bundleName, "start_recording");
            var stopRecordingSFX = Calls.LoadAssetFromStream<AudioClip>(this, bundleName, "stop_recording");
            Log($"Finished loading assets");

            OBS_SFX_Players = new GameObject("OBS_SFX_Players");
            GameObject.DontDestroyOnLoad(OBS_SFX_Players);
            screenshotSFXPlayer = createAudioPlayer(screenshotSFX, "screenshotSFXPlayer", 1);
            confirmationSFXPlayer = createAudioPlayer(confirmationSFX, "confirmationSFXPlayer", 0.6f);
            startRecordingSFXPlayer = createAudioPlayer(startRecordingSFX, "startRecordingSFXPlayer", 0.2f);
            stopRecordingSFXPlayer = createAudioPlayer(stopRecordingSFX, "stopRecordingSFXPlayer", 0.2f);
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
                LoadAssets();
                InitClient();
                SetUIOptions();
                OnUISaved();
                UI.instance.UI_Initialized += OnUIInit;
            }
        }

        /**
         * <summary>
         * (Re)start connection thread.
         * </summary>
         */
        public static void Connect()
        {
            if (IsConnected())
            {
                Disconnect();
            }
            connectionManager.UpdateWebsocketConfig(ip, port, password);
            connectionManager.Start();
        }

        /**
         * <summary>
         * Close the connection and don't try to reconnect.
         * </summary>
         */
        public static void Disconnect()
        {
            connectionManager.Stop();
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
         * Specify the different options that will be used in the ModUI settings
         * </summary>
         */
        private void SetUIOptions()
        {
            Mod.ModName = BuildInfo.ModName;
            Mod.ModVersion = BuildInfo.ModVersion;

            Mod.SetFolder("OBS_Control_API");
            Mod.AddToList("Force enable replay buffer", true, 0, "Never forget to start the replay buffer again!\n" +
                "The mod will start it for you on connection, and stop it as you close the game.", new Tags { });
            Mod.AddToList("IP address", "localhost", "IP address of the OBS websocket server.", new Tags { });
            Mod.AddToList("Port", 4455, "Port used by the OBS websocket server.", new Tags { });
            Mod.AddToList("Password", "", "Password for the OBS websocket server.", new Tags { });
            Mod.AddToList("Key binding: both left buttons", "Save replay buffer",
                    "Action to perform when both buttons on the left controller are being pressed.\n" +
                    "Possible values:\n" +
                    "- Nothing\n" +
                    "- Save replay buffer\n" +
                    "- Start recording\n" +
                    "- Stop recording\n" +
                    "- Toggle recording\n" +
                    "- Save screenshot", new Tags { });
            Mod.AddToList("Key binding: both right buttons", "Nothing",
                    "Action to perform when both buttons on the right controller are being pressed.\n" +
                    "Possible values:\n" +
                    "- Nothing\n" +
                    "- Save replay buffer\n" +
                    "- Start recording\n" +
                    "- Stop recording\n" +
                    "- Toggle recording\n" +
                    "- Save screenshot", new Tags { });
            Mod.AddToList("Haptic feedback duration", 0.2f, "Duration of the haptic impulse when an action is successful (set to 0 to disable).", new Tags { });
            Mod.AddToList("Audio feedback", true, 0, "Set to true to get a sound effect when an action is successful", new Tags { });
            Mod.GetFromFile();
        }

        /**
         * <summary>
         * Called when the actual ModUI window is initialized
         * </summary>
         */
        private void OnUIInit()
        {
            Mod.ModSaved += OnUISaved;
            UI.instance.AddMod(Mod);
        }

        /**
         * <summary>
         * Called when the user saves a configuration in ModUI
         * </summary>
         */
        private void OnUISaved()
        {
            forceReplayBuffer = (bool)Mod.Settings[0].SavedValue;
            ip = (string)Mod.Settings[1].SavedValue;
            port = (int)Mod.Settings[2].SavedValue;
            password = (string)Mod.Settings[3].SavedValue;
            keyBindings[0] = (string)Mod.Settings[4].SavedValue;
            keyBindings[1] = (string)Mod.Settings[5].SavedValue;
            hapticsDuration = (float)Mod.Settings[6].SavedValue;
            enableSFX = (bool)Mod.Settings[7].SavedValue;
            Connect();
        }

        /**
         * <summary>
         * Executes an action that is connected to a key binding.
         * </summary>
         */
        private void ExecuteKeyBinding(int index)
        {
            bindingLocked[index] = true;
            bool success = false;
            GameObject audioPlayer = null;
            switch (keyBindings[index])
            {
                case "Save replay buffer":
                    if(SaveReplayBuffer())
                    {
                        Log($"Saved replay buffer");
                        audioPlayer = confirmationSFXPlayer;
                        success = true;
                    }
                    break;
                case "Start recording":
                    if (StartRecord())
                    {
                        Log($"Started recording");
                        audioPlayer = startRecordingSFXPlayer;
                        success = true;
                    }
                    break;
                case "Stop recording":
                    {
                        var res = StopRecord();
                        if (res != null)
                        {
                            Log($"Stopped recording, saved to: {res.outputPath}");
                            audioPlayer = stopRecordingSFXPlayer;
                            success = true;
                        }
                    }
                    break;
                case "Toggle recording":
                    {
                        var res = ToggleRecord();
                        if (res != null)
                        {
                            var active = res.outputActive;
                            string toggleValue = active ? "Started" : "Stopped";
                            Log($"{toggleValue} recording");
                            audioPlayer = active ? startRecordingSFXPlayer : stopRecordingSFXPlayer;
                            success = true;
                        }
                    }
                    break;
                case "Save screenshot":
                    if (SaveSourceScreenshot())
                    {
                        Log($"Saved screenshot");
                        audioPlayer = screenshotSFXPlayer;
                        success = true;
                    }
                    break;
                default:
                    Log($"Invalid key binding \"{keyBindings[index]}\"");
                    break;
            }
            if (success)
            {
                Feedback(audioPlayer);
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
         * If needed, perform a feedback to notify the user of an event.
         * </summary>
         */
        private void Feedback(GameObject audioPlayer)
        {
            if (enableSFX && audioPlayer is not null)
            {
                audioPlayer.GetComponent<AudioSource>().Play();
            }
            if (hapticsDuration > 0)
            {
                HapticFeedback(1, hapticsDuration);
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
         * Play the screenshot sound effect (camera shutter sound)
         * </summary>
         */
        public static void playScreenshotSFX()
        {
            screenshotSFXPlayer.GetComponent<AudioSource>().Play();
        }

        /**
         * <summary>
         * Play the "start recording" sound effect
         * </summary>
         */
        public static void playStartRecordingSFX()
        {
            startRecordingSFXPlayer.GetComponent<AudioSource>().Play();
        }

        /**
         * <summary>
         * Play the "stop recording" sound effect
         * </summary>
         */
        public static void playStopRecordingSFX()
        {
            stopRecordingSFXPlayer.GetComponent<AudioSource>().Play();
        }


        /**
         * <summary>
         * Perform a haptic impulse on both controllers.
         * </summary>
         */
        public static void HapticFeedback(float intensity, float duration)
        {
            if (playerHaptics != null)
            {
                playerHaptics.PlayControllerHaptics(intensity, duration, intensity, duration);
            }
        }

        /**
         * <summary>
         * Called 50 times per second, used for frequent updates.
         * </summary>
         */
        public override void OnFixedUpdate()
        {
            if (!bindingLocked[0] && keyBindings[0] != "Nothing")
            {
                if (Calls.ControllerMap.LeftController.GetPrimary() > 0 && Calls.ControllerMap.LeftController.GetSecondary() > 0)
                {
                    Log($"Left key binding activated");
                    //ExecuteKeyBinding(0);
                    new Thread(() =>
                    {
                        ExecuteKeyBinding(0);
                    }).Start();
                }
            }
            if (!bindingLocked[1] && keyBindings[1] != "Nothing")
            {
                if (Calls.ControllerMap.RightController.GetPrimary() > 0 && Calls.ControllerMap.RightController.GetSecondary() > 0)
                {
                    Log($"Right key binding activated");
                    //ExecuteKeyBinding(1);
                    new Thread(() =>
                    {
                        ExecuteKeyBinding(1);
                    }).Start();
                }
            }
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
            Disconnect();
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
