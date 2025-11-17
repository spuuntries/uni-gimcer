var MediaPipePlugin = {
    $MediaPipeState: {
        videoElement: null,
        hands: null,
        camera: null,
        callbackGameObjectName: null,
        callbackMethodName: null,
        
        start: function() {
            console.log('MediaPipe: start() called');
            MediaPipeState.hands = new window.Hands({
                locateFile: (file) => {
                    console.log('MediaPipe: locating file:', file);
                    return `https://cdn.jsdelivr.net/npm/@mediapipe/hands/${file}`;
                }
            });

            MediaPipeState.hands.setOptions({
                maxNumHands: 1,
                modelComplexity: 1,
                minDetectionConfidence: 0.5,
                minTrackingConfidence: 0.5
            });
            console.log('MediaPipe: hands options set');

            MediaPipeState.hands.onResults(function(results) {
                var handsCount = (results.multiHandLandmarks && results.multiHandLandmarks.length) ? results.multiHandLandmarks.length : 0;
                console.log('MediaPipe: onResults called, hands detected:', handsCount);
                MediaPipeState.onResults(results);
            });

            MediaPipeState.camera = new window.Camera(MediaPipeState.videoElement, {
                onFrame: async () => {
                    await MediaPipeState.hands.send({ image: MediaPipeState.videoElement });
                },
                width: 640,
                height: 480
            });
            console.log('MediaPipe: camera created, starting...');
            MediaPipeState.camera.start();
            console.log('MediaPipe: camera started');
        },
        
        onResults: function(results) {
            if (results.multiHandLandmarks && MediaPipeState.callbackGameObjectName && MediaPipeState.callbackMethodName) {
                if (results.multiHandLandmarks.length > 0) {
                    const landmarks = JSON.stringify(results.multiHandLandmarks[0]);
                    console.log('MediaPipe: sending landmarks to Unity:', landmarks.substring(0, 100) + '...');
                    SendMessage(MediaPipeState.callbackGameObjectName, MediaPipeState.callbackMethodName, landmarks);
                }
            } else {
                console.log('MediaPipe: no hands detected or callback not set');
            }
        }
    },

    MediaPipe_Init: function(gameObjectName, methodName) {
        console.log('MediaPipe: MediaPipe_Init called');
        MediaPipeState.callbackGameObjectName = UTF8ToString(gameObjectName);
        MediaPipeState.callbackMethodName = UTF8ToString(methodName);
        console.log('MediaPipe: callback set to', MediaPipeState.callbackGameObjectName, '.', MediaPipeState.callbackMethodName);

        // Add compatibility shim for Module.arguments
        if (typeof Module !== 'undefined' && !Module.arguments) {
            console.log('MediaPipe: Adding Module.arguments compatibility shim');
            Module.arguments = [];
        }

        MediaPipeState.videoElement = document.createElement('video');
        //MediaPipeState.videoElement.style.display = 'none'; // Hide the video element
        document.body.appendChild(MediaPipeState.videoElement);
        console.log('MediaPipe: video element created and added to DOM');

        const files = [
            'https://cdn.jsdelivr.net/npm/@mediapipe/camera_utils/camera_utils.js',
            'https://cdn.jsdelivr.net/npm/@mediapipe/drawing_utils/drawing_utils.js',
            'https://cdn.jsdelivr.net/npm/@mediapipe/hands/hands.js'
        ];

        let loadedFiles = 0;
        const onFileLoaded = () => {
            loadedFiles++;
            console.log('MediaPipe: loaded file', loadedFiles, 'of', files.length);
            if (loadedFiles === files.length) {
                console.log('MediaPipe: all files loaded, calling start()');
                MediaPipeState.start();
            }
        };

        files.forEach(file => {
            console.log('MediaPipe: loading script:', file);
            const script = document.createElement('script');
            script.src = file;
            script.crossOrigin = 'anonymous';
            script.onload = onFileLoaded;
            script.onerror = (error) => {
                console.error('MediaPipe: failed to load script:', file, error);
            };
            document.body.appendChild(script);
        });
    }
};

autoAddDeps(MediaPipePlugin, '$MediaPipeState');
mergeInto(LibraryManager.library, MediaPipePlugin);
