using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HullcamVDS;
using OfCourseIStillLoveYou.Client;
using UnityEngine;

namespace OfCourseIStillLoveYou
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Core : MonoBehaviour
    {
        public static Dictionary<int, TrackingCamera> TrackedCameras = new Dictionary<int, TrackingCamera>();

        private void Awake()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    GrpcClient.ConnectToServer(Settings.EndPoint, Settings.Port);
                }
                catch (System.Exception ex)
                {
                    Log($"Failed to connect to GRPC Server: {ex.Message}");
                }
            });
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
                cam.CalculateSpeedAltitude();
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
