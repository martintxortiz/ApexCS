using Grpc.Core;
using UnityEngine;
using OfCourseIStillLoveYou.Communication;

namespace OfCourseIStillLoveYou.Client
{
    public static class GrpcClient
    {
        public static CameraStream.CameraStreamClient Client { get; set; }

        public static void ConnectToServer(string endpoint = "localhost", int port = 50777)
        {
            var channel = new Channel(endpoint, port, ChannelCredentials.Insecure);

            Client = new CameraStream.CameraStreamClient(channel);

            Debug.Log("[OfCourseIStillLoveYou]: GrpcClient Connected to Server");
        }

        public static async void SendCameraTextureAsync(CameraData cameraData)
        {
            try
            {
                await Client.SendCameraStreamAsync(new SendCameraStreamRequest()
                {
                    CameraId = cameraData.CameraId,
                    CameraName = cameraData.CameraName,
                    Speed = cameraData.Speed,
                    Altitude = cameraData.Altitude,
                    Texture = Google.Protobuf.ByteString.CopyFrom(cameraData.Texture)
                });
            }
            catch (System.Exception ex)
            {
                Debug.Log($"[OfCourseIStillLoveYou]: Failed to send camera data: {ex.Message}");
            }
        }

    }
}
