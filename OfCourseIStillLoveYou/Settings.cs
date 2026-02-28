using System;
using UnityEngine;

namespace OfCourseIStillLoveYou
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class Settings : MonoBehaviour
    {
        public static string SettingsConfigUrl = "GameData/OfCourseIStillLoveYou/settings.cfg";
        public static int Port { get; set; }
        public static string EndPoint { get; set; }
        public static bool ConfigLoaded { get; set; }
        public static int Width { get; set; } = 1280;
        public static int Height { get; set; } = 720;
        public static bool AutoStartStreaming { get; set; } = true;
        public static int MjpegPort { get; set; } = 8181;

        void Awake()
        {
            LoadConfig();
            ConfigLoaded = true;
        }

        public static void LoadConfig()
        {
            try
            {
                Debug.Log("[OfCourseIStillLoveYou]: Loading settings.cfg ==");

                ConfigNode fileNode = ConfigNode.Load(SettingsConfigUrl);
                if (fileNode == null) 
                {
                    Debug.Log("[OfCourseIStillLoveYou]: settings.cfg not found, using default values.");
                    return;
                }
                if (!fileNode.HasNode("Settings")) return;

                ConfigNode settings = fileNode.GetNode("Settings");
                EndPoint = settings.GetValue("EndPoint") ?? "localhost";
                
                string portStr = settings.GetValue("Port");
                if (!string.IsNullOrEmpty(portStr) && int.TryParse(portStr, out int p))
                    Port = p;

                string wStr = settings.GetValue("Width");
                if (!string.IsNullOrEmpty(wStr) && int.TryParse(wStr, out int w))
                    Width = w;
                
                string hStr = settings.GetValue("Height");
                if (!string.IsNullOrEmpty(hStr) && int.TryParse(hStr, out int h))
                    Height = h;

                string autoStr = settings.GetValue("AutoStartStreaming");
                if (!string.IsNullOrEmpty(autoStr) && bool.TryParse(autoStr, out bool a))
                    AutoStartStreaming = a;

                string mjpegStr = settings.GetValue("MjpegPort");
                if (!string.IsNullOrEmpty(mjpegStr) && int.TryParse(mjpegStr, out int m))
                    MjpegPort = m;
            }
            catch (Exception ex)
            {
                Debug.Log("[OfCourseIStillLoveYou]: Failed to load settings config:" + ex.Message);
            }
        }
    }
}
