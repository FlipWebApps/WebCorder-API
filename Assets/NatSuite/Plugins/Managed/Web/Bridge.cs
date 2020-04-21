/* 
*   WebCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

namespace NatSuite.Web.Internal {

    using System;
    using System.Runtime.InteropServices;

    public static class Bridge {

        public delegate void CompletionHandler (IntPtr context, IntPtr path);

        #if UNITY_WEBGL
        [DllImport(Assembly, EntryPoint = @"WCRecording")]
        public static extern bool Recording ();
        [DllImport(Assembly, EntryPoint = @"WCStartRecording")]
        public static extern void StartRecording (int width, int height, float framerate, int sampleRate, int channelCount, int bitrate, CompletionHandler callback, IntPtr context);
        [DllImport(Assembly, EntryPoint = @"WCCommitFrame")]
        public static extern void CommitFrame (IntPtr pixelBuffer);
        [DllImport(Assembly, EntryPoint = @"WCCommitSamples")]
        public static extern void CommitSamples (float[] sampleBuffer, int sampleCount);
        [DllImport(Assembly, EntryPoint = @"WCStopRecording")]
        public static extern void StopRecording ();
        #else

        public static bool Recording () => false;
        public static void StartRecording (int width, int height, float framerate, int sampleRate, int channelCount, int bitrate, CompletionHandler callback, IntPtr context) { }
        public static void CommitFrame (IntPtr pixelBuffer) { }
        public static void CommitSamples (float[] sampleBuffer, int sampleCount) { }
        public static void StopRecording () { }
        #endif
    }
}