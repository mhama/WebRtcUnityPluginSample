using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePeerConnectionM;
using System;
using System.Runtime.InteropServices;
using System.Threading;

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
    /*
    List<byte[]> bufferYUVLocal = new List<byte[]> {
        new byte[1920 * 1080],
        new byte[1920 * 1080],
        new byte[1920 * 1080]
    };
    List<byte[]> bufferYUVRemote = new List<byte[]> {
        new byte[1920 * 1080],
        new byte[1920 * 1080],
        new byte[1920 * 1080]
    };
    byte[] bufferLocal = new byte[1920*1080];
    byte[] bufferRemote = new byte[1920*1080];
    */
    System.Random random = new System.Random();


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

    void InitWebRTC() {

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
        PeerConnectionM peer = new PeerConnectionM(new List<string>(), "user", "cred");
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

    // Update is called once per frame
    void Update() {
		if (first)
        {
            first = false;
            InitWebRTC();
        }
    }
    public void OnLocalSdpReadytoSend(int id, string type, string sdp) {
        Debug.Log("OnLocalSdpReadytoSend called. id="+id+" | type="+type+" | sdp="+sdp);
    }

    public void OnIceCandiateReadytoSend(int id, string candidate, int sdpMlineIndex, string sdpMid) {
        Debug.Log("OnIceCandiateReadytoSend called. id="+id+" candidate="+candidate+" sdpMid="+sdpMid);
    }

    public void OnI420LocalFrameReady(int id,
            IntPtr dataY, IntPtr dataU, IntPtr dataV,
            int strideY, int strideU, int strideV,
            uint width, uint height)
    {
        
        Debug.Log("OnI420LocalFrameReady called! w=" + width + " h=" + height+" thread:"+ Thread.CurrentThread.ManagedThreadId + ":" + Thread.CurrentThread.Name);
        // added some wait...
        double x = 1.0;
        for (int i = 0; i < 4000000; i++)
        {
            x += random.NextDouble();
        }

        FramePacket packet = frameQueueLocal.GetDataBufferWithoutContents((int) (width * height * 4));
        Debug.Log("CopyYuvToBuffer start.");
        CopyYuvToBuffer(dataY, dataU, dataV, strideY, strideU, strideV, width, height, packet.Buffer);
        //Debug.Log("CopyYuvToBuffer end.");
        packet.width = (int)width;// + (int)Math.Floor(x * 0.00000001);
        packet.height = (int)height;
        frameQueueLocal.Push(packet);
        //Debug.Log("framePacket Pushed.");
    }

    public void OnI420RemoteFrameReady(int id,
        IntPtr dataY, IntPtr dataU, IntPtr dataV,
        int strideY, int strideU, int strideV,
        uint width, uint height)
    {
        Debug.Log("OnI420RemoteFrameReady called! w=" + width + " h=" + height + " thread:" + Thread.CurrentThread.ManagedThreadId);
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
            for (int i = 0; i < height / 2 ; i++)
            {
                srcOffsetY = i * strideY;
                srcOffsetU = (i/2) * strideU;
                srcOffsetV = (i/2) * strideV;
                destOffset = i * (int)width * 4;
                for (int j = 0; j < width / 2 ; j+=2)
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
                    Debug.LogError("candidate is null!");
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