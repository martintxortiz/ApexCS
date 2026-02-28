using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OfCourseIStillLoveYou
{
    /// <summary>
    /// Serves live JPEG frames over HTTP as an MJPEG stream.
    /// Endpoints:
    /// - GET /list     -> Returns JSON array of available camera instance IDs.
    /// - GET /cam/{id} -> Provides the MJPEG stream for the given ID.
    /// </summary>
    public static class MjpegServer
    {
        private static readonly ConcurrentDictionary<string, byte[]> _latestFrames =
            new ConcurrentDictionary<string, byte[]>();

        private static HttpListener _listener;
        private static Thread _thread;
        private static volatile bool _running;

        public static void Start(int port)
        {
            if (_running) return;

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _running = true;

            _thread = new Thread(ListenLoop) { IsBackground = true, Name = "ApexMjpegServer" };
            _thread.Start();

            Debug.Log($"[Apex]: MJPEG server started on http://localhost:{port}/");
        }

        public static void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            _latestFrames.Clear();
            Debug.Log("[Apex]: MJPEG server stopped.");
        }

        public static void ClearFrames()
        {
            _latestFrames.Clear();
        }

        // Called from TrackingCamera after each frame is encoded.
        public static void PushFrame(string cameraId, byte[] jpegBytes)
        {
            _latestFrames[cameraId] = jpegBytes;
        }

        private static void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    Task.Run(() => Serve(ctx));
                }
                catch { /* listener stopped */ }
            }
        }

        private static void Serve(HttpListenerContext ctx)
        {
            var path = ctx.Request.Url.AbsolutePath; // e.g. /cam/12345

            // GET /list  → JSON array of active camera IDs
            if (path == "/list")
            {
                var json = "[" + string.Join(",", _latestFrames.Keys) + "]";
                var body = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = body.Length;
                ctx.Response.OutputStream.Write(body, 0, body.Length);
                ctx.Response.Close();
                return;
            }

            // GET /cam/{id}  → MJPEG stream (multipart/x-mixed-replace)
            // cv2.VideoCapture reads this natively.
            var cameraId = path.TrimStart('/').Replace("cam/", "");
            ctx.Response.ContentType = "multipart/x-mixed-replace; boundary=apexframe";
            ctx.Response.StatusCode = 200;
            ctx.Response.SendChunked = true;

            var output = ctx.Response.OutputStream;
            while (_running)
            {
                try
                {
                    if (_latestFrames.TryGetValue(cameraId, out var frame) && frame != null)
                    {
                        // MJPEG frame header
                        var header = Encoding.ASCII.GetBytes(
                            "--apexframe\r\n" +
                            "Content-Type: image/jpeg\r\n" +
                            $"Content-Length: {frame.Length}\r\n\r\n");

                        output.Write(header, 0, header.Length);
                        output.Write(frame, 0, frame.Length);
                        output.Write(new byte[] { 0x0D, 0x0A }, 0, 2); // \r\n
                        output.Flush();
                    }

                    Thread.Sleep(33); // ~30 fps cap
                }
                catch { break; }
            }
            try { ctx.Response.Close(); } catch { }
        }
    }
}
