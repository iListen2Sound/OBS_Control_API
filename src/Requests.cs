using MelonLoader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace OBS_Control_API
{
    public partial class OBS
    {
        /**
         * <summary>
         * Generic method to send a request with parameters to OBS WebSocket and get the response as a string.
         * The return value is null if the request failed (e.g. OBS not running, wrong parameters, etc.).
         * If not null, the parsing of the response is up to the caller, as different requests have different response formats.
         * </summary>
         */
        public static string SendRequest(string requestType, object parameters)
        {
            return requestManager.SendRequest(requestType, parameters);
        }
        /**
         * <summary>
         * Generic method to send a request without parameters to OBS WebSocket and get the response as a string.
         * The return value is null if the request failed (e.g. OBS not running, wrong parameters, etc.).
         * If not null, the parsing of the response is up to the caller, as different requests have different response formats.
         * </summary>
         */
        public static string SendRequest(string requestType)
        {
            return SendRequest(requestType, null);
        }

        /**
         * <summary>
         * Fetches the current status of the replay buffer.
         * </summary>
         */
        public static RequestResponse.GetReplayBufferStatus GetReplayBufferStatus()
        {
            var resp = SendRequest("GetReplayBufferStatus");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetReplayBufferStatus>(resp);
        }
        /**
         * <summary>
         * Fetches the current recording status.
         * </summary>
         */
        public static RequestResponse.GetRecordStatus GetRecordStatus()
        {
            var resp = SendRequest("GetRecordStatus");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetRecordStatus>(resp);
        }
        /**
         * <summary>
         * Fetches the current streaming status.
         * </summary>
         */
        public static RequestResponse.GetStreamStatus GetStreamStatus()
        {
            var resp = SendRequest("GetStreamStatus");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetStreamStatus>(resp);
        }
        /**
         * <summary>
         * Fetches the version of OBS and the negociated protocol parameters.
         * </summary>
         */
        public static RequestResponse.GetVersion GetVersion()
        {
            var resp = SendRequest("GetVersion");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetVersion>(resp);
        }
        /**
         * <summary>
         * Starts the stream.
         * </summary>
         */
        public static bool StartStream()
        {
            var resp = SendRequest("StartStream");
            return !(resp is null);
        }
        /**
         * <summary>
         * Stops the stream.
         * </summary>
         */
        public static bool StopStream()
        {
            var resp = SendRequest("StopStream");
            return !(resp is null);
        }
        /**
         * <summary>
         * Starts the replay buffer.
         * </summary>
         */
        public static bool StartReplayBuffer()
        {
            var resp = SendRequest("StartReplayBuffer");
            return !(resp is null);
        }
        /**
         * <summary>
         * Stops the replay buffer.
         * </summary>
         */
        public static bool StopReplayBuffer()
        {
            var resp = SendRequest("StopReplayBuffer");
            return !(resp is null);
        }
        /**
         * <summary>
         * Saves the replay buffer.
         * </summary>
         */
        public static bool SaveReplayBuffer()
        {
            var resp = SendRequest("SaveReplayBuffer");
            return !(resp is null);
        }
        /**
         * <summary>
         * Gets the name of the file that the replay buffer was saved to last time.
         * </summary>
         */
        public static RequestResponse.GetLastReplayBufferReplay GetLastReplayBufferReplay()
        {
            var resp = SendRequest("GetLastReplayBufferReplay");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetLastReplayBufferReplay>(resp);
        }
        /**
         * <summary>
         * Starts recording.
         * </summary>
         */
        public static bool StartRecord()
        {
            var resp = SendRequest("StartRecord");
            return !(resp is null);
        }
        /**
         * <summary>
         * Stops recording.
         * </summary>
         */
        public static RequestResponse.StopRecord StopRecord()
        {
            var resp = SendRequest("StopRecord");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.StopRecord>(resp);
        }
        /**
         * <summary>
         * Toggles the recording status. Stops it if it was running, starts it if it wasn't. Doesn't do anything if it's in the "stopping" state.
         * </summary>
         */
        public static RequestResponse.ToggleRecord ToggleRecord()
        {
            var resp = SendRequest("ToggleRecord");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.ToggleRecord>(resp);
        }
        /**
         * <summary>
         * Pauses recording.
         * </summary>
         */
        public static bool PauseRecord()
        {
            var resp = SendRequest("PauseRecord");
            return !(resp is null);
        }
        /**
         * <summary>
         * Resumes recording.
         * </summary>
         */
        public static bool ResumeRecord()
        {
            var resp = SendRequest("ResumeRecord");
            return !(resp is null);
        }
        /**
         * <summary>
         * Splits the current file being recorded into a new file.
         * </summary>
         */
        public static bool SplitRecordFile()
        {
            var resp = SendRequest("SplitRecordFile");
            return !(resp is null);
        }
        /**
         * <summary>
         * Gets the current directory that recording is saved to.
         * </summary>
         */
        public static RequestResponse.GetRecordDirectory GetRecordDirectory()
        {
            var resp = SendRequest("GetRecordDirectory");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetRecordDirectory>(resp);
        }
        /**
         * <summary>
         * Sets a new directory to write recording files to.
         * </summary>
         */
        public static bool SetRecordDirectory(string recordDirectory)
        {
            var parameters = new Dictionary<string, object>
            {
                ["recordDirectory"] = recordDirectory
            };
            var resp = SendRequest("SetRecordDirectory", parameters);
            return !(resp is null);
        }
        /**
         * <summary>
         * ???
         * </summary>
         */
        public static RequestResponse.GetCurrentProgramScene GetCurrentProgramScene()
        {
            var resp = SendRequest("GetCurrentProgramScene");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetCurrentProgramScene>(resp);
        }
        /**
         * <summary>
         * Save a new screenshot from the current scene. The file has an auto-generated timestamp name.
         * </summary>
         */
        public static bool SaveSourceScreenshot()
        {
            string separator = isWindows ? "\\" : "/";
            string fileName = $"Screenshot {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.png";
            return SaveSourceScreenshot(recordingDirectory + separator + fileName);
        }
        /**
         * <summary>
         * Save a new screenshot from the current scene to a specific file.
         * </summary>
         */
        public static bool SaveSourceScreenshot(string imageFilePath)
        {
            return SaveSourceScreenshot(sceneUuid, imageFilePath);
        }
        /**
         * <summary>
         * Save a new screenshot from a specific source and to a specific file.
         * </summary>
         */
        public static bool SaveSourceScreenshot(string sourceUuid, string imageFilePath)
        {
            var parameters = new Dictionary<string, object>
            {
                ["sourceUuid"] = sourceUuid,
                ["imageFormat"] = "png",
                ["imageFilePath"] = imageFilePath
            };
            var resp = SendRequest("SaveSourceScreenshot", parameters);
            return !(resp is null);
        }
        /**
         * <summary>
         * Get the list of scenes , their names and UUIDs, and the current program and preview scenes.
         * </summary>
         */
        public static RequestResponse.GetSceneList GetSceneList()
        {
            var resp = SendRequest("GetSceneList");
            if (resp is null) return null;
            return JsonConvert.DeserializeObject<RequestResponse.GetSceneList>(resp);
        }
        /**
         * <summary>
         * Sets the current program scene by its name.
         * </summary>
         */
        public static bool SetCurrentProgramScene(string sceneName)
        {
            var parameters = new Dictionary<string, object> { ["sceneName"] = sceneName };
            var resp = SendRequest("SetCurrentProgramScene", parameters);
            return !(resp is null);
        }
        /**
         * <summary>
         * Sets the current program scene by its UUID.
         * </summary>
         */
        public static bool SetCurrentProgramSceneByUuid(string sceneUuid)
        {
            var parameters = new Dictionary<string, object> { ["sceneUuid"] = sceneUuid };
            var resp = SendRequest("SetCurrentProgramScene", parameters);
            return !(resp is null);
        }
    }
}

namespace OBS_Control_API
{
    public partial class RequestResponse
    {
        public class GetReplayBufferStatus
        {
            public bool outputActive { get; set; }
        }
        public class ToggleRecord
        {
            public bool outputActive { get; set; }
        }
        public class StopRecord
        {
            public string outputPath { get; set; }
        }
        public class GetRecordStatus
        {
            public bool outputActive { get; set; }
            public bool outputPaused { get; set; }
            public string outputTimecode { get; set; }
            public int outputDuration { get; set; }
            public long outputBytes { get; set; }
        }
        public class GetStreamStatus
        {
            public bool outputActive { get; set; }
            public bool outputReconnecting { get; set; }
            public string outputTimecode { get; set; }
            public int outputDuration { get; set; }
            public float outputCongestion { get; set; }
            public long outputBytes { get; set; }
            public int outputSkippedFrames { get; set; }
            public int outputTotalFrames { get; set; }
        }
        public class GetVersion
        {
            public string obsVersion { get; set; }
            public string obsWebSocketVersion { get; set; }
            public int rpcVersion { get; set; }
            public string[] availableRequests { get; set; }
            public string[] supportedImageFormats { get; set; }
            public string platform { get; set; }
            public string platformDescription { get; set; }
        }
        public class GetLastReplayBufferReplay
        {
            public string savedReplayPath { get; set; }
        }
        public class GetRecordDirectory
        {
            public string recordDirectory { get; set; }
        }
        public class GetCurrentProgramScene
        {
            public string sceneName { get; set; }
            public string sceneUuid { get; set; }
            public string currentProgramSceneName { get; set; }
            public string currentProgramSceneUuid { get; set; }
        }
        public class Scene
        {
            public int sceneIndex { get; set; }
            public string sceneName { get; set; }
            public string sceneUuid { get; set; }
        }
        public class GetSceneList
        {
            public string currentProgramSceneName { get; set; }
            public string currentProgramSceneUuid { get; set; }
            public string currentPreviewSceneName { get; set; }
            public string currentPreviewSceneUuid { get; set; }
            public Scene[] scenes { get; set; }
        }
    }

}
