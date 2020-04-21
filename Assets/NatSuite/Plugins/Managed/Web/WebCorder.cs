/* 
*   WebCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatSuite.Web {

    using AOT;
    using UnityEngine;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Internal;

    /// <summary>
    /// </summary>
    public static class WebCorder {
        
        #region --Client API--
        /// <summary>
        /// </summary>
        public static bool Recording => Bridge.Recording();

        /// <summary>
        /// </summary>
        public static void StartRecording (int width, int height, float framerate, int sampleRate = 0, int channelCaount = 0, int bitrate = (int)(960 * 540 * 11.4)) {
            recordingTask = new TaskCompletionSource<string>();
            readbackBuffer = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var handle = GCHandle.Alloc(recordingTask, GCHandleType.Normal);
            Bridge.StartRecording(width, height, framerate, sampleRate, channelCaount, bitrate, OnRecording, (IntPtr)handle);
        }

        /// <summary>
        /// </summary>
        public static void CommitFrame<T> (T[] pixelBuffer) where T : struct {
            var handle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
            CommitFrame(handle.AddrOfPinnedObject());
            handle.Free();
        }

        /// <summary>
        /// </summary>
        public static void CommitFrame (IntPtr nativeBuffer) => Bridge.CommitFrame(nativeBuffer);

        /// <summary>
        /// </summary>
        public static void CommitFrame (params Camera[] cameras) { // NatCorder's `CameraInput` in one function :D
            // Check
            if (cameras.Length == 0)
                return;
            // Render every camera
            var frameBuffer = RenderTexture.GetTemporary(readbackBuffer.width, readbackBuffer.height, 24);
            Array.Sort(cameras, (a, b) => (int)(10 * (a.depth - b.depth)));
            for (var i = 0; i < cameras.Length; i++) {
                var prevTarget = cameras[i].targetTexture;
                cameras[i].targetTexture = frameBuffer;
                cameras[i].Render();
                cameras[i].targetTexture = prevTarget;
            }
            // Readback
            var prevActive = RenderTexture.active;
            RenderTexture.active = frameBuffer;
            readbackBuffer.ReadPixels(new Rect(0, 0, readbackBuffer.width, readbackBuffer.height), 0, 0, false);
            RenderTexture.active = prevActive;
            // Commit
            CommitFrame(readbackBuffer.GetPixels32());
        }

        /// <summary>
        /// </summary>
        public static Task<string> StopRecording () {
            Bridge.StopRecording();
            Texture2D.Destroy(readbackBuffer);
            return recordingTask.Task;
        }
        #endregion


        #region --Operations---

        private static TaskCompletionSource<string> recordingTask;
        private static Texture2D readbackBuffer;

        [MonoPInvokeCallback(typeof(Bridge.CompletionHandler))]
        private static void OnRecording (IntPtr context, IntPtr path) {
            // Get task
            var handle = (GCHandle)context;
            var recordingTask = handle.Target as TaskCompletionSource<string>;
            handle.Free();
            // Invoke completion task
            if (path != IntPtr.Zero)
                recordingTask.SetResult(Marshal.PtrToStringAnsi(path));
            else
                recordingTask.SetException(new Exception(@"WebCorder failed to stop recording"));
        }
        #endregion
    }
}