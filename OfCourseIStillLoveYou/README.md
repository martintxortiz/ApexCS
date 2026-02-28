# Apex Camera System (KSP)

Apex is a powerful camera extension and streaming system for Kerbal Space Program. It allows you to monitor your spacecraft's on-board cameras and effortlessly stream those live camera feeds to external programs—ideal for flight software, computer vision tasks (like OpenCV), or custom telemetry dashboards.

> Apex is a fork of the original **OfCourseIStillLoveYou (OCISLY)** mod. It builds upon OCISLY's foundation by strictly cleanly separating the UI from the streaming backend, enabling robust headless streams and server endpoints.

## Features

- **Decoupled MJPEG Streaming Server:** Stream camera feeds in the background over HTTP directly to OpenCV or Python, even when the in-game camera windows are closed.
- **Dynamic Field of View (FOV):** Seamlessly syncs with HullcamVDS FOV settings.
- **In-Game Configuration:** Adjust resolution, HTTP stream ports, and streaming states on the fly from the Apex Toolbar UI.
- **Professional Architecture:** Automatically hooks into flight scenes, correctly unregisters ghosts on scene changes, and drops dependencies on KSP's native UI elements for robust integration.

## How to Connect Programmatically (Python / OpenCV)

Apex runs an MJPEG HTTP server locally. Once a camera is actively transmitting (indicated by a **Green** light in the KSP Apex Toolbar UI), you can capture its video feed using standard CV2 in Python.

### 1. List Available Cameras
You can view a list of all currently streaming camera IDs by sending a standard GET request to `/list`.

```python
import requests

# Fetch the list of active camera IDs from Apex
response = requests.get("http://localhost:8181/list")
camera_ids = response.json()
print("Available Cameras:", camera_ids)
# Output: Available Cameras: ['2411', '8910']
```
*(If you experience connection errors, make sure KSP is in an active Flight scene and the Apex MJPEG server port matches your configuration).*

### 2. Stream a Camera
You can read from the stream natively with `cv2.VideoCapture()`. Pass the `http://localhost:8181/cam/{cameraId}` URL directly to OpenCV.

```python
import cv2

# Use one of the camera IDs from the /list endpoint
camera_id = "2411"
url = f"http://localhost:8181/cam/{camera_id}"

cap = cv2.VideoCapture(url)

while True:
    ret, frame = cap.read()
    if not ret:
        print("Stream ended or failed to connect.")
        break
        
    cv2.imshow('Apex Camera Stream', frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()
```

## Configuration

You can configure Apex directly from the Flight Scene toolbar:
1. Click the **Apex Camera System** icon.
2. Click **Show Config**.
3. Adjust the **Width / Height** to your preferring processing resolution.
4. Set the **MJPEG Port** (Default `8181`) or **gRPC Port** if running custom external binaries. 

*(Changes to ports require a game restart to bind correctly).*

## Dependencies
- [HullcamVDS Continued](https://forum.kerbalspaceprogram.com/topic/145633-112x-hullcam-vds-continued/)
- ModuleManager
