using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HullcamVDS;

using UnityEngine;

namespace OfCourseIStillLoveYou
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Core : MonoBehaviour
    {
        public static Dictionary<int, TrackingCamera> TrackedCameras = new Dictionary<int, TrackingCamera>();

        private void Awake()
        {
            Log("Apex Core Awake - Initializing services...");


            try
            {
                // Attempt to bind to all interfaces; fallback to localhost if permission denied
                MjpegServer.Start(Settings.MjpegPort);
            }
            catch (System.Exception e)
            {
                Log($"Failed to start MJPEG server: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            Log("Apex Core Destroy - Tearing down services...");
            foreach (var cam in TrackedCameras.Values)
            {
                cam.Disable();
            }
            TrackedCameras.Clear();
            MjpegServer.ClearFrames();
        }

        private IEnumerator Start()
        {
            // Wait until KSP flight scene is fully ready before scanning for cameras.
            yield return new WaitUntil(() => FlightGlobals.ready);

            AutoRegisterAllCameras();
        }

        // Registers every hull camera found on all loaded vessels and, if
        // AutoStartStreaming is enabled in settings, activates streaming immediately.
        private void AutoRegisterAllCameras()
        {
            foreach (var camera in GetAllTrackingCameras())
            {
                int id = camera.GetInstanceID();
                if (TrackedCameras.ContainsKey(id)) continue;

                var tracked = new TrackingCamera(id, camera);
                tracked.StreamingEnabled = Settings.AutoStartStreaming;
                TrackedCameras.Add(id, tracked);

                Log($"Camera registered: {camera.cameraName} | Streaming: {tracked.StreamingEnabled}");
            }
        }

        public static void Log(string message)
        {
            Debug.Log($"[OfCourseIStillLoveYou]: {message}");
        }

        public static List<MuMechModuleHullCamera> GetAllTrackingCameras()
        {
            var result = new List<MuMechModuleHullCamera>();
            if (!FlightGlobals.ready) return result;

            foreach (var vessel in FlightGlobals.VesselsLoaded)
                result.AddRange(vessel.FindPartModulesImplementing<MuMechModuleHullCamera>());

            return result;
        }

        void Update()
        {
            ToggleRender();
        }

        void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            foreach (var cam in TrackedCameras.Values.Where(c => c.Enabled))
            {
                if (!cam.OddFrames) continue;
                cam.SendCameraImage();
            }
        }

        private void ToggleRender()
        {
            foreach (var cam in TrackedCameras.Values.Where(c => c.Enabled))
                cam.ToogleCameras();
        }
    }
}
