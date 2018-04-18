using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePeerConnectionM;
using System;
using System.Runtime.InteropServices;
using System.Threading;

public class WebRtcNativeCallSample : MonoBehaviour {

    bool first = true;
    Texture2D textureLocal;
    Texture2D textureRemote;
    public Material localTargetMaterial;
    public Material remoteTargetMaterial;
    public FrameQueue frameQueueLocal = new FrameQueue(3);
    public FrameQueue frameQueueRemote = new FrameQueue(5);
    public WebRtcVideoPlayer localPlayer;
    public WebRtcVideoPlayer remotePlayer;
    public string status;

    PeerConnectionM peer;
    string offerSdp;
    string answerSdp;
    List<string> iceCandidates;


    // Use this for initialization
    void Start() {
        Debug.Log("Sample.Start() + " + " thread: " + Thread.CurrentThread.ManagedThreadId + ":" + Thread.CurrentThread.Name);
        if (localPlayer != null)
        {
            localPlayer.frameQueue = frameQueueLocal;
        }
        if (remotePlayer != null)
        {
            remotePlayer.frameQueue = frameQueueRemote;
        }
    }

    public void InitWebRTC() {
        iceCandidates = new List<string>();

#if UNITY_ANDROID
        AndroidJavaClass systemClass = new AndroidJavaClass("java.lang.System");
        string libname = "jingle_peerconnection_so";
        systemClass.CallStatic("loadLibrary", new object[1] { libname });
        Debug.Log("loadLibrary loaded : " + libname);

        /*
         * Below is equivalent of this java code:
         * PeerConnectionFactory.InitializationOptions.Builder builder = 
         *   PeerConnectionFactory.InitializationOptions.builder(UnityPlayer.currentActivity);
         * PeerConnectionFactory.InitializationOptions options = 
         *   builder.createInitializationOptions();
         * PeerConnectionFactory.initialize(options);
         */

        AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaClass webrtcClass = new AndroidJavaClass("org.webrtc.PeerConnectionFactory");
        AndroidJavaClass initOptionsClass = new AndroidJavaClass("org.webrtc.PeerConnectionFactory$InitializationOptions");
        AndroidJavaObject builder = initOptionsClass.CallStatic<AndroidJavaObject>("builder", new object[1] { activity });
        AndroidJavaObject options = builder.Call<AndroidJavaObject>("createInitializationOptions");
        if (webrtcClass != null)

        {
            Debug.Log("PeerConnectionFactory.initialize calling");
            webrtcClass.CallStatic("initialize", new object[1] { options });
            Debug.Log("PeerConnectionFactory.initialize called.");
        }

#endif
        List<string> servers = new List<string>();
        servers.Add("stun: stun.skyway.io:3478");
        servers.Add("stun: stun.l.google.com:19302");
        peer = new PeerConnectionM(servers, "", "");
        int id = peer.GetUniqueId();
        Debug.Log("PeerConnectionM.GetUniqueId() : " + id);

        peer.OnLocalSdpReadytoSend += OnLocalSdpReadytoSend;
        peer.OnIceCandiateReadytoSend += OnIceCandiateReadytoSend;
        peer.OnLocalVideoFrameReady += OnI420LocalFrameReady;
        peer.OnRemoteVideoFrameReady += OnI420RemoteFrameReady;
        peer.OnFailureMessage += OnFailureMessage;
    }

    WebRtcSocket socket;
    public string serverURL = "http://00000000.ngrok.io";

    public void ConnectToServer()
    {
        //if (socket == null)
        {
            socket = new WebRtcSocket();
            socket.OnOffer += (WebRtcMsg msg) =>
            {
                status = "Offer received.";
                Debug.Log("SetRemoteDescription(offer) sdp:" + msg.msg);
                peer.SetRemoteDescription("offer", msg.msg);
                peer.CreateAnswer();
                //socket.Emit("webrtc-answer", sdp);
            };
            socket.OnAnswer += (WebRtcMsg msg) =>
            {
                status = "Answer received.";
                //socket.Emit("webrtc-offer", sdp);
                Debug.Log("SetRemoteDescription(answer) sdp:"+ msg.msg);
                peer.SetRemoteDescription("answer", msg.msg);
                foreach(string candidate in iceCandidates)
                {
                    socket.Emit("webrtc-icecandidate", candidate);
                }
                iceCandidates.Clear();
            };
            socket.OnJoin += (WebRtcMsg msg) =>
            {
                status = "Joined";
            };
            socket.OnWelcome += (WebRtcMsg msg) =>
            {
                if (this.offerSdp != null)
                {
                    socket.Emit("webrtc-offer", offerSdp);
                }
            };
            socket.OnExit += (WebRtcMsg msg) =>
            {
                status = "Exited";
            };
            socket.OnConnect += () =>
            {
                status = "Connected";
            };
            socket.OnConnectError += (msg) =>
            {
                status = "Connect error: "+msg;
            };
            socket.OnDisconnect += () =>
            {
                status = "Disconnected";
            };
            status = "Connecting...";
            socket.Open(serverURL);
        }
    }

    public void Close()
    {
        if (peer != null)
        {
            peer.ClosePeerConnection();
            peer = null;
        }
    }

    public void OfferWithCamera()
    {
        Close();
        InitWebRTC();
        if (peer != null)
        {
            peer.AddStream(false);
            Debug.Log("calling peer.CreateOffer()");
            peer.CreateOffer();
            Debug.Log("called peer.CreateOffer()");
        }
    }

    public void OfferWithoutCamera()
    {
        Close();
        InitWebRTC();
        if (peer != null)
        {
            Debug.Log("calling peer.CreateOffer()");
            peer.CreateOffer();
            Debug.Log("called peer.CreateOffer()");
        }
    }

    // Update is called once per frame
    void Update() {
    }

    public void OnLocalSdpReadytoSend(int id, string type, string sdp) {
        Debug.Log("OnLocalSdpReadytoSend called. id="+id+" | type="+type+" | sdp="+sdp);
        // send offer

        if (type == "offer")
        {
            if (socket != null)
            {
                socket.Emit("webrtc-offer", sdp);
                status = "Offer sent.";
            }
            else
            {
                this.offerSdp = sdp;
            }
        }
        else if (type == "answer")
        {
            if (socket != null)
            {
                socket.Emit("webrtc-answer", sdp);
                status = "Answer sent.";
            }
            else
            {
                this.answerSdp = sdp;
            }
        }
        else
        {
            Debug.Log("Unknown type : " + type);
        }
    }

    public void OnIceCandiateReadytoSend(int id, string candidate, int sdpMlineIndex, string sdpMid) {
        Debug.Log("OnIceCandiateReadytoSend called. id="+id+" candidate="+candidate+" sdpMid="+sdpMid);
        if (socket != null)
        {
            socket.Emit("webrtc-icecandidate", candidate);
        } else
        {
            iceCandidates.Add(candidate);
        }
    }

    public void OnI420LocalFrameReady(int id,
            IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
            int strideY, int strideU, int strideV, int strideA,
            uint width, uint height)
    {
        
        //Debug.Log("OnI420LocalFrameReady called! w=" + width + " h=" + height+" thread:"+ Thread.CurrentThread.ManagedThreadId + ":" + Thread.CurrentThread.Name);
        FramePacket packet = frameQueueLocal.GetDataBufferWithoutContents((int) (width * height * 4));
        if (packet == null)
        {
            //Debug.LogError("OnI420LocalFrameReady: FramePacket is null!");
            return;
        }
        CopyYuvToBuffer(dataY, dataU, dataV, strideY, strideU, strideV, width, height, packet.Buffer);
        packet.width = (int)width;
        packet.height = (int)height;
        frameQueueLocal.Push(packet);
    }

    public void OnI420RemoteFrameReady(int id,
        IntPtr dataY, IntPtr dataU, IntPtr dataV, IntPtr dataA,
        int strideY, int strideU, int strideV, int strideA,
        uint width, uint height)
    {
        //Debug.Log("OnI420RemoteFrameReady called! w=" + width + " h=" + height + " thread:" + Thread.CurrentThread.ManagedThreadId);
        FramePacket packet = frameQueueRemote.GetDataBufferWithoutContents((int)(width * height * 4));
        if (packet == null)
        {
            Debug.LogError("OnI420RemoteFrameReady: FramePacket is null!");
            return;
        }
        CopyYuvToBuffer(dataY, dataU, dataV, strideY, strideU, strideV, width, height, packet.Buffer);
        packet.width = (int)width;
        packet.height = (int)height;
        frameQueueRemote.Push(packet);
    }

    void CopyYuvToBuffer(IntPtr dataY, IntPtr dataU, IntPtr dataV,
        int strideY, int strideU, int strideV,
        uint width, uint height, byte[] buffer)
    {
        unsafe
        {
            byte* ptrY = (byte*)dataY.ToPointer();
            byte* ptrU = (byte*)dataU.ToPointer();
            byte* ptrV = (byte*)dataV.ToPointer();
            int srcOffsetY = 0;
            int srcOffsetU = 0;
            int srcOffsetV = 0;
            int destOffset = 0;
            for (int i = 0; i < height ; i++)
            {
                srcOffsetY = i * strideY;
                srcOffsetU = (i/2) * strideU;
                srcOffsetV = (i/2) * strideV;
                destOffset = i * (int)width * 4;
                for (int j = 0; j < width ; j+=2)
                {
                    {
                        byte y = ptrY[srcOffsetY];
                        byte u = ptrU[srcOffsetU];
                        byte v = ptrV[srcOffsetV];
                        srcOffsetY++;
                        srcOffsetU++;
                        srcOffsetV++;
                        destOffset += 4;
                        buffer[destOffset] = y;
                        buffer[destOffset + 1] = u;
                        buffer[destOffset + 2] = v;
                        buffer[destOffset + 3] = 0xff;
                    
                        // use same u, v values
                        byte y2 = ptrY[srcOffsetY];
                        srcOffsetY++;
                        destOffset += 4;
                        buffer[destOffset] = y2;
                        buffer[destOffset + 1] = u;
                        buffer[destOffset + 2] = v;
                        buffer[destOffset + 3] = 0xff;
                    }
                }
            }
        }
    }

    public void OnFailureMessage(int id, string msg)
    {
        Debug.Log("OnFailureMessage called! id=" + id + " msg=" + msg);
    }
}
