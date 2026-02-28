using System.Collections.Generic;
using HullcamVDS;
using KSP.UI.Screens;
using UnityEngine;

namespace OfCourseIStillLoveYou
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class Gui : MonoBehaviour
    {
        private const string ModTitle = "Apex Camera System";

        private const float WindowWidth = 280;
        private const float DraggableHeight = 40;
        private const float LeftIndent = 12;
        private const float ContentTop = 20;
        public static Gui Fetch;
        public static bool GuiEnabled;
        public static bool HasAddedButton;
        private const float ContentWidth = WindowWidth - 2 * LeftIndent;
        private const float EntryHeight = 22;
        private bool _gameUiToggle;
        private float _windowHeight = 250;
        private Rect _windowRect;

        private bool _showConfig = false;
        private string _cfgWidth, _cfgHeight, _cfgMjpegPort;

        private static readonly GUIStyle CenterLabelStyle = new GUIStyle()
            { alignment = TextAnchor.UpperCenter, normal = { textColor = Color.white } };

        private static readonly GUIStyle TitleStyle = new GUIStyle(CenterLabelStyle)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        private Texture2D _greenTex;
        private Texture2D _redTex;

        void Awake()
        {
            if (Fetch) Destroy(Fetch);
            Fetch = this;

            _greenTex = MakeSolidColorTexture(16, 16, new Color(0.2f, 0.8f, 0.2f));
            _redTex = MakeSolidColorTexture(16, 16, new Color(0.8f, 0.2f, 0.2f));

            _cfgWidth = Settings.Width.ToString();
            _cfgHeight = Settings.Height.ToString();
            _cfgMjpegPort = Settings.MjpegPort.ToString();
        }

        private Texture2D MakeSolidColorTexture(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        void Start()
        {
            _windowRect = new Rect(Screen.width - WindowWidth - 40, 100, WindowWidth, _windowHeight);
            AddToolbarButton();
            GameEvents.onHideUI.Add(GameUiDisable);
            GameEvents.onShowUI.Add(GameUiEnable);
            _gameUiToggle = true;
        }

        void OnGUI()
        {
            if (GuiEnabled && _gameUiToggle)
            {
                _windowRect = GUI.Window(1850, _windowRect, GuiWindow, "");
            }
            // Need to update cameras regardless of toolbar window visibility so popups stay open
            UpdateAllCameras();
        }

        void LateUpdate()
        {
            RemoveDestroyedCameras();
        }

        private void RemoveDestroyedCameras()
        {
            var camerasToDelete = new List<int>();

            foreach (var trackingCamera in Core.TrackedCameras)
            {
                if (trackingCamera.Value.Vessel == null || !trackingCamera.Value.Vessel.loaded)
                {
                    trackingCamera.Value.Disable();
                    camerasToDelete.Add(trackingCamera.Value.Id);
                }
            }

            foreach (var cameraId in camerasToDelete) Core.TrackedCameras.Remove(cameraId);
        }

        private void GuiWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, WindowWidth, DraggableHeight));
            float curY = ContentTop;

            GUI.Label(new Rect(0, 0, WindowWidth, 20), ModTitle, TitleStyle);
            curY += 25;

            // Config Toggle
            if (GUI.Button(new Rect(LeftIndent, curY, ContentWidth, EntryHeight), _showConfig ? "Hide Config" : "Show Config"))
            {
                _showConfig = !_showConfig;
            }
            curY += EntryHeight + 5;

            if (_showConfig)
            {
                DrawConfigPanel(ref curY);
            }

            // Draw Camera List
            foreach (var camKV in Core.TrackedCameras)
            {
                DrawCameraRow(camKV.Value, ref curY);
            }

            _windowHeight = curY + 20;
            _windowRect.height = _windowHeight;
        }

        private void DrawConfigPanel(ref float curY)
        {
            GUI.Label(new Rect(LeftIndent, curY, 80, 20), "Width");
            _cfgWidth = GUI.TextField(new Rect(LeftIndent + 80, curY, ContentWidth - 80, 20), _cfgWidth);
            curY += 22;

            GUI.Label(new Rect(LeftIndent, curY, 80, 20), "Height");
            _cfgHeight = GUI.TextField(new Rect(LeftIndent + 80, curY, ContentWidth - 80, 20), _cfgHeight);
            curY += 22;

            GUI.Label(new Rect(LeftIndent, curY, 80, 20), "MJPEG Port");
            _cfgMjpegPort = GUI.TextField(new Rect(LeftIndent + 80, curY, ContentWidth - 80, 20), _cfgMjpegPort);
            curY += 22;

            if (GUI.Button(new Rect(LeftIndent, curY, ContentWidth, 20), "Apply Config & Restart Server"))
            {
                int.TryParse(_cfgWidth, out int w);
                int.TryParse(_cfgHeight, out int h);
                int.TryParse(_cfgMjpegPort, out int cp);
                Settings.Width = w > 0 ? w : Settings.Width;
                Settings.Height = h > 0 ? h : Settings.Height;
                
                if (cp > 0 && cp != Settings.MjpegPort)
                {
                    Settings.MjpegPort = cp;
                    // Dynamically restart the server if the port changed
                    MjpegServer.Stop();
                    MjpegServer.Start(Settings.MjpegPort);
                }
            }
            curY += 25;
        }

        private void DrawCameraRow(TrackingCamera camera, ref float curY)
        {
            // Indicator box
            var tex = camera.StreamingEnabled ? _greenTex : _redTex;
            GUI.DrawTexture(new Rect(LeftIndent, curY + 4, 12, 12), tex);

            // Name label (truncated if long)
            string name = camera.Name;
            if (name.Length > 18) name = name.Substring(0, 16) + "..";
            GUI.Label(new Rect(LeftIndent + 18, curY + 2, 110, 20), name);

            // Toggle Stream Button
            if (GUI.Button(new Rect(LeftIndent + 125, curY, 55, 18), camera.StreamingEnabled ? "Stop" : "Stream"))
            {
                camera.StreamingEnabled = !camera.StreamingEnabled;
            }

            // Toggle View Button
            if (GUI.Button(new Rect(LeftIndent + 185, curY, 55, 18), camera.WindowOpen ? "Hide" : "View"))
            {
                camera.WindowOpen = !camera.WindowOpen;
                // Ensure it's marked as minimally UI on open by default to limit screen clutter
                if (camera.WindowOpen) camera.MinimalUi = true;
            }

            curY += 22;
        }

        private void UpdateAllCameras()
        {
            foreach (var trackingCamera in Core.TrackedCameras.Values)
            {
                // Update tracking UI logic regardless of toolbar state
                if (trackingCamera.Enabled && trackingCamera.WindowOpen)
                {
                    trackingCamera.CheckIfResizing();
                    trackingCamera.CreateGui();
                }
            }
        }

        private void AddToolbarButton()
        {
            if (!HasAddedButton)
            {
                Texture buttonTexture = GameDatabase.Instance.GetTexture("OfCourseIStillLoveYou/Textures/icon", false);
                ApplicationLauncher.Instance.AddModApplication(EnableGui, DisableGui, Dummy, Dummy, Dummy, Dummy,
                    ApplicationLauncher.AppScenes.FLIGHT, buttonTexture);
                HasAddedButton = true;
            }
        }

        private void EnableGui()
        {
            GuiEnabled = true;
            Core.Log("Showing GUI");
        }

        private void DisableGui()
        {
            GuiEnabled = false;
            Core.Log("Hiding GUI");
        }

        private void Dummy() { }
        private void GameUiEnable() { _gameUiToggle = true; }
        private void GameUiDisable() { _gameUiToggle = false; }
    }
}
