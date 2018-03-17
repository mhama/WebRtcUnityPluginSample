using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePeerConnectionM;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Quobject.Collections.Immutable;
//using socket.io;


public class WebRtcMsg
{
    public int id;
    public string msg;
};

public class WebRtcSocket
{
    int myUserId = 0;
    Socket socket;

    public int MyUserId
    {
        get { return myUserId; }
    }

    public WebRtcSocket()
    {
    }

    public void Open(string url)
    {
        //System.Net.ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(OnRemoteCertificateValidationCallback)
        //System.Net.ServicePointManager.ServerCertificateValidationCallback = (p1, p2, p3, p4) => true;

        //Socket socket = Socket.Connect(url);
        Debug.Log("SocketIO: connecting to:" + url);
        IO.Options opts = new IO.Options();
        opts.Transports = ImmutableList<string>.Empty.Add("polling");
        Socket socket = IO.Socket(url, opts);

        //socket.On(SystemEvents.connect, () =>
        socket.On(Socket.EVENT_CONNECT, () =>
        {
            Debug.Log("SocketIO: connected!");
            this.socket = socket;
            InitHandlers();
            this.OnConnect();
        });
        /*
        socket.On(SystemEvents.reconnect, () =>
        {
            Debug.Log("SocketIO: reconnected!");
            //this.OnConnect();
        });
        */
        //socket.On(SystemEvents.disconnect, () =>
        socket.On(Socket.EVENT_DISCONNECT, () =>
        {
            Debug.Log("SocketIO: disconnected!");
            this.OnDisconnect();
            this.socket = null;
        });
        //socket.Connect();
    }

    void InitHandlers()
    {
        socket.On("welcome", (data) =>
        {
            Debug.Log("SocketIO: welcome");
            WebRtcMsg msg = ConvertDataToWebRtcMsg(data);
            myUserId = msg.id;
            this.OnWelcome(msg);
        });
        socket.On("webrtc-offer", (data) =>
        {
            WebRtcMsg msg = ConvertDataToWebRtcMsg(data);
            if (msg.id == myUserId) return;
            Debug.Log("SocketIO: webrtc-offer");
            this.OnOffer(msg);
        });
        socket.On("webrtc-answer", (data) =>
        {
            WebRtcMsg msg = ConvertDataToWebRtcMsg(data);
            if (msg.id == myUserId) return;
            Debug.Log("SocketIO: webrtc-answer");
            this.OnAnswer(msg);
        });
        socket.On("join", (data) =>
        {
            WebRtcMsg msg = ConvertDataToWebRtcMsg(data);
            if (msg.id == myUserId) return;
            Debug.Log("SocketIO: join");
            this.OnJoin(msg);
        });
        socket.On("exit", (data) =>
        {
            WebRtcMsg msg = ConvertDataToWebRtcMsg(data);
            if (msg.id == myUserId) return;
            Debug.Log("SocketIO: exit");
            this.OnExit(msg);
        });
    }

    public void Emit(string msgType, string body)
    {
        //WebRtcMsg msg = new WebRtcMsg();
        //msg.id = myUserId;
        //msg.body = body;
        Debug.Log("SocketIO: Emit msgType:"+ msgType+" {id:" + myUserId+", msg:"+body+"}");
        //string payload = JsonConvert.SerializeObject(msg);
        this.socket.Emit(msgType, body);
    }

    WebRtcMsg ConvertDataToWebRtcMsg(object data)
    {
        string str = data.ToString();
        WebRtcMsg msg = JsonConvert.DeserializeObject<WebRtcMsg>(str);
        //string strChatLog = "user#" + msg.id + ": " + msg.body;
        return msg;
    }

    public delegate void MessageListener(WebRtcMsg msg);
    public delegate void ConnectionListener();

    public event MessageListener OnOffer;
    public event MessageListener OnAnswer;
    public event MessageListener OnJoin;
    public event MessageListener OnExit;
    public event MessageListener OnWelcome;
    public event ConnectionListener OnConnect;
    public event ConnectionListener OnDisconnect;

    private bool OnRemoteCertificateValidationCallback(
  object sender,
  X509Certificate certificate,
  X509Chain chain,
  SslPolicyErrors sslPolicyErrors)
    {
        return true;  // 「SSL証明書の使用は問題なし」と示す
    }
}

public class WebRTCNaticeCallSample : MonoBehaviour {

    bool first = true;
    Texture2D textureLocal;
    Texture2D textureRemote;
    public Material localTargetMaterial;
    public Material remoteTargetMaterial;
    public FrameQueue frameQueueLocal = new FrameQueue(3);
    public FrameQueue frameQueueRemote = new FrameQueue(5);
    public WebRTCVideoPlayer localPlayer;
    public WebRTCVideoPlayer remotePlayer;

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

        peer.AddStream(false);
        peer.CreateOffer();

        //peer.SetRemoteDescription()
        Debug.Log("PeerConnectionM.CreateOffer() called.");

    }

    WebRtcSocket socket;
    public string serverURL = "https://e9f5618d.ngrok.io";

    public void InitSocketIo()
    {
        //if (socket == null)
        {
            socket = new WebRtcSocket();
            socket.OnOffer += (WebRtcMsg msg) =>
            {
                Debug.Log("SetRemoteDescription(offer) sdp:" + msg.msg);
                peer.SetRemoteDescription("offer", msg.msg);
                peer.CreateAnswer();
                //socket.Emit("webrtc-answer", sdp);
            };
            socket.OnAnswer += (WebRtcMsg msg) =>
            {
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
                //socket.Emit("webrtc-answer", sdp);
            };
            socket.OnConnect += () =>
            {

            };
            socket.OnDisconnect += () =>
            {
            };
            socket.Open(serverURL);
        }
    }

    // Update is called once per frame
    void Update() {
		if (first)
        {
            first = false;
            //InitWebRTC();
        }
    }
    public void OnLocalSdpReadytoSend(int id, string type, string sdp) {
        Debug.Log("OnLocalSdpReadytoSend called. id="+id+" | type="+type+" | sdp="+sdp);
        // send offer

        if (type == "offer")
        {
            if (socket != null)
            {
                socket.Emit("webrtc-offer", sdp);
            } else
            {
                this.offerSdp = sdp;
            }
        }
        else if (type == "answer")
        {
            if (socket != null)
            {
                socket.Emit("webrtc-answer", sdp);
            } else
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
        Debug.Log("OnI420RemoteFrameReady called! w=" + width + " h=" + height + " thread:" + Thread.CurrentThread.ManagedThreadId);
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
        /*
        int size = (int)width * (int)height;
        Marshal.Copy(dataY, bufferYUV[0], 0, size);
        Marshal.Copy(dataU, bufferYUV[1], 0, size);
        Marshal.Copy(dataV, bufferYUV[2], 0, size);
        textureRemote.SetPixels(0, 0, width, height, );
        */

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
                    
                    
                        byte y2 = ptrY[srcOffsetY];
                        //byte u = ptrU[srcOffsetU];
                        //byte v = ptrV[srcOffsetV];
                        srcOffsetY++;
                        //srcOffsetU++;
                        //srcOffsetV++;
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

public class FramePacket
{
    public FramePacket(int bufsize)
    {
        _buffer = new byte[bufsize];
    }

    public int width;
    public int height;
    private byte[] _buffer;
    public byte[] Buffer {
        get { return _buffer; }
    }

    public override string ToString()
    {
        return "FramePacket width, height=("+width+","+height+") buffer size:"+_buffer.Length;
    }
}

// byte[] のキュー。
// 指定サイズ以上Pushすると、指定サイズ以下になるよう末尾のデータから削除される。
// スレッドセーフ。
public class FrameQueue
{
    private Deque<FramePacket> frames = new Deque<FramePacket>();
    private FramePacketPool bufferPool = new FramePacketPool();
    private int maxQueueCount;
    MovieStats stats = new MovieStats();

    public FrameQueue(int _maxQueueCount)
    {
        maxQueueCount = _maxQueueCount;
    }

    public void Push(FramePacket frame)
    {
        stats.CountFrameLoad();
        FramePacket trashBuf = null;
        lock (this)
        {
            frames.AddFront(frame);
            if (frames.Count >= maxQueueCount)
            {
                stats.CountFrameSkip();
                trashBuf = frames.RemoveBack();
            }
        }
        // lock内でPushしないのは、thisとbufferPoolの両方のlockを同時にとらないようにする配慮。
        if (trashBuf != null)
        {
            bufferPool.Push(trashBuf);
        }
    }

    public FramePacket Pop()
    {
        lock (this)
        {
            if (frames.IsEmpty)
            {
                return null;
            }
            stats.CountFrameShow();
            return frames.RemoveBack();
        }
    }

    public FramePacket GetDataBufferWithContents(int width, int height, byte[] src, int size)
    {
        return bufferPool.GetDataBufferWithContents(width, height, src, size);
    }

    public FramePacket GetDataBufferWithoutContents(int size)
    {
        return bufferPool.GetDataBuffer(size);
    }

    public void Pool(FramePacket buf)
    {
        bufferPool.Push(buf);
    }

    public int Count
    {
        get
        {
            lock (this)
            {
                return frames.Count;
            }
        }
    }

    public FramePacketPool FramePacketPool
    {
        get { return bufferPool; }
    }

    public MovieStats Stats
    {
        get { return stats; }
    }
}

public class MovieStats
{
    const int maxSamples = 100;
    private Deque<float> frameLoadTimes = new Deque<float>(maxSamples + 1);
    private Deque<float> frameShowTimes = new Deque<float>(maxSamples + 1);
    private Deque<float> frameSkipTimes = new Deque<float>(maxSamples + 1);
    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

    public MovieStats()
    {
        sw.Start();
    }

    public void CountFrameLoad()
    {
        AddSampleToQueue(frameLoadTimes);
    }
    public void CountFrameSkip()
    {
        AddSampleToQueue(frameSkipTimes);
    }
    public void CountFrameShow()
    {
        AddSampleToQueue(frameShowTimes);
    }
    void AddSampleToQueue(Deque<float> queue)
    {
        lock (this)
        {
            queue.AddFront(sw.ElapsedMilliseconds * 0.001f);
            if (queue.Count >= maxSamples)
            {
                queue.RemoveBack();
            }
        }
    }
    public float fpsLoad()
    {
        return CalcFps(frameLoadTimes);
    }
    public float fpsSkip()
    {
        return CalcFps(frameSkipTimes);
    }
    public float fpsShow()
    {
        return CalcFps(frameShowTimes);
    }
    public float CalcFps(Deque<float> queue)
    {
        int count = 0;
        float firstTime = 0;
        float lastTime = 0;
        lock (this)
        {
            count = queue.Count;
            if (count >= 2)
            {
                firstTime = queue.Get(0);
                lastTime = queue.Get(count - 1);
            }
        }
        if (count <= 1)
        {
            return 0;
        }
        float fps = (count - 1) / (firstTime - lastTime);
        return fps;
    }
}

// 返却されたバッファをできるだけ再利用するバッファプール。
// スレッドセーフ。
public class FramePacketPool
{
    private Deque<FramePacket> pool = new Deque<FramePacket>();

    // リクエストされたサイズ以上のバッファを返す。
    public FramePacket GetDataBuffer(int size)
    {
        lock (this)
        {
            // TODO: １回だけでいいの？
            if (pool.Count > 0)
            {
                FramePacket candidate = pool.RemoveFront();
                if (candidate == null)
                {
                    Debug.LogError("candidate is null! returns new buffer.");
                    return GetNewBuffer(size);
                } else
                {
                    if (candidate.Buffer == null)
                    {
                        Debug.LogError("candidate.Buffer is null!");
                    }
                }
                if (candidate.Buffer.Length > size)
                {
                    return candidate;
                }
            }
        }
        return GetNewBuffer(size);
    }

    private FramePacket GetNewBuffer(int neededSize)
    {
        FramePacket packet = new FramePacket((int)(neededSize * 1.2));
        return packet;
    }

    // バッファをプールから取り出し、さらにデータをコピーして渡す
    public FramePacket GetDataBufferWithContents(int width, int height, byte[] src, int size)
    {
        FramePacket dest = GetDataBuffer(size);
        System.Array.Copy(src, 0, dest.Buffer, 0, size);
        dest.width = width;
        dest.height = height;
        return dest;
    }

    // 返却
    public void Push(FramePacket packet)
    {
        lock (this)
        {
            pool.AddFront(packet);
        }
    }
}