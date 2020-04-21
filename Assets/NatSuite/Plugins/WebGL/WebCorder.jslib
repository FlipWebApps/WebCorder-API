/* 
*   WebCorder
*   Copyright (c) 2020 Yusuf Olokoba.
*/

const WebCorder = {

    $sharedInstance : {
        recordingCallback: null,
        recordingContext: null,
        framebuffer: null,
        framebufferContext: null,
        pixelBuffer: null,
        audioContext: null,
        audioStream: null,
        recorder: null,
        MIME_TYPE: "video/webm", // const
    },

    WCStartRecording : function (width, height, framerate, sampleRate, channelCount, bitrate, recordingCallback, context) {
        sharedInstance.framebuffer = document.createElement("canvas");
        sharedInstance.framebuffer.width = width;
        sharedInstance.framebuffer.height = height;
        sharedInstance.framebufferContext = sharedInstance.framebuffer.getContext("2d");
        sharedInstance.pixelBuffer = sharedInstance.framebufferContext.getImageData(0, 0, width, height);
        const videoStream = sharedInstance.framebuffer.captureStream(framerate);
        const tracks = [videoStream.getVideoTracks()[0]];
        if (sampleRate > 0 && channelCount > 0) {
            sharedInstance.audioContext = new AudioContext({ latencyHint: "interactive", sampleRate });
            sharedInstance.audioStream = sharedInstance.audioContext.createMediaStreamDestination({ channelCount, channelCountMode: "explicit" });
            tracks.push(sharedInstance.audioStream.stream.getAudioTracks()[0]);
        }
        const options = { mimeType : sharedInstance.MIME_TYPE, videoBitsPerSecond : bitrate };
        sharedInstance.recorder = new MediaRecorder(new MediaStream(tracks), options);
        sharedInstance.recordingCallback = recordingCallback;
        sharedInstance.recordingContext = context;
        sharedInstance.recorder.start();
        console.log("WebCorder: Starting recording");
        return 1;
    },

    WCCommitFrame : function (pixelBuffer) {
        // Invert
        var w = sharedInstance.pixelBuffer.width;
        var h = sharedInstance.pixelBuffer.height;
        var s = w * 4;
        for (var i = 0; i < h; i++)
            sharedInstance.pixelBuffer.data.set(new Uint8Array(HEAPU8.buffer, pixelBuffer + (h - i - 1) * s, s), i * s);
        // Commit
        sharedInstance.framebufferContext.putImageData(sharedInstance.pixelBuffer, 0, 0);
    },

    WCCommitSamples : function (sampleBuffer, sampleCount) {
        const audioBuffer = sharedInstance.audioContext.createBuffer(sharedInstance.channelCount, sampleCount / sharedInstance.channelCount, sharedInstance.sampleRate);
        sampleBuffer = new Float32Array(HEAPU8.buffer, sampleBuffer, sampleCount);
        for (var c = 0; c < audioBuffer.numberOfChannels; c++) {
            const channelData = audioBuffer.getChannelData(c);
            for (var i = 0; i < audioBuffer.length; i++)
                channelData[i] = sampleBuffer[i * audioBuffer.numberOfChannels + c];
        }
        var audioSource = sharedInstance.audioContext.createBufferSource();
        audioSource.buffer = audioBuffer;
        audioSource.connect(sharedInstance.audioStream);
        audioSource.start();
    },

    WCStopRecording : function () {
        console.log("WebCorder: Stopping recording");
        sharedInstance.recorder.ondataavailable = function (e) {
            const videoBlob = new Blob([e.data], { "type": sharedInstance.MIME_TYPE });
            const videoURL = URL.createObjectURL(videoBlob);
            console.log("WebCorder: Completed recording video", videoBlob, "to URL:", videoURL);
            const pathSize = lengthBytesUTF8(videoURL) + 1;
            const path = _malloc(pathSize);
            stringToUTF8(videoURL, path, pathSize);
            Runtime.dynCall("vii", sharedInstance.recordingCallback, [sharedInstance.recordingContext, path]);
            _free(path);
        };
        sharedInstance.recorder.stop();
        if (sharedInstance.audioContext)
            sharedInstance.audioContext.close();
        sharedInstance.recorder = null;
        sharedInstance.framebuffer = null;
        sharedInstance.framebufferContext = null;
        sharedInstance.pixelBuffer = null;
        sharedInstance.audioContext = null;
    }
};

autoAddDeps(WebCorder, "$sharedInstance");

mergeInto(LibraryManager.library, WebCorder);