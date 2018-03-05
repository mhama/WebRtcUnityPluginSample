using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebRTCVideoPlayer : MonoBehaviour {

    private Texture2D tex;
    public FrameQueue frameQueue; // WebRTCNativeCallSampleがセットする。
    float lastUpdateTime;
    byte[] buffer = null;

    [SerializeField]
    private bool _playing;
    [SerializeField]
    private bool _failed;
    [SerializeField]
    private float _fpsLoad;
    [SerializeField]
    private float _fpsShow;
    [SerializeField]
    private float _fpsSkip;

    // Use this for initialization
    void Start () {
        tex = new Texture2D(2, 2);
        tex.SetPixel(0, 0, Color.blue);
        tex.SetPixel(1, 1, Color.blue);
        tex.Apply();
        GetComponent<Renderer>().material.mainTexture = tex;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.fixedTime - lastUpdateTime > 1.0 / 31.0)
        {
            lastUpdateTime = Time.fixedTime;
            TryProcessFrame();
        }

        _fpsLoad = frameQueue.Stats.fpsLoad();
        _fpsShow = frameQueue.Stats.fpsShow();
        _fpsSkip = frameQueue.Stats.fpsSkip();
    }

    private void TryProcessFrame()
    {
        if (frameQueue != null)
        {
            FramePacket packet = frameQueue.Pop();
            Debug.Log((packet == null ? "no frame to consume." : "frame consumed.") + "framesCount : " + frameQueue.Count);
            if (packet != null)
            {
                ProcessFrameBuffer(packet);
                frameQueue.Pool(packet);
            }
        }
    }

    private void ProcessFrameBuffer(FramePacket packet)
    {
        if (packet == null) {
            return;
        }

        if (tex == null || (tex.width != packet.width || tex.height != packet.height)) {
            Debug.Log("Create Texture. width:"+packet.width+" height:"+packet.height);
            tex = new Texture2D(packet.width, packet.height, TextureFormat.RGBA32, false);
            //tex = new RenderTexture(packet.width, packet.height, 0, RenderTextureFormat.BGRA32, RenderTextureReadWrite.Default);
            buffer = new byte[packet.width * packet.height * 4];
        }
        Debug.Log("Received Packet. " + packet.ToString());
        if (packet.Buffer.Length > 8)
        {
            Debug.Log("buffer: " +
                + packet.Buffer[0] + ","
                + packet.Buffer[1] + ","
                + packet.Buffer[2] + ","
                + packet.Buffer[3] + ","
                + packet.Buffer[4] + ","
                + packet.Buffer[5] + ","
                + packet.Buffer[6] + ","
                + packet.Buffer[7]
                );
        }
        Array.Copy(packet.Buffer, 0, buffer, 0, buffer.Length);
        Debug.Log("call LoadRawTextureData");
        tex.LoadRawTextureData(buffer);
        //tex.LoadRawTextureData(packet.Buffer);
        double x = 1.0;
        for(int i=0; i < 1000000; i++)
        {
            x += UnityEngine.Random.Range(0, 0.1f);
        }
        Debug.Log("call Apply "+Math.Floor(x * 0.00010));
        tex.Apply();
        Debug.Log("set Main Texture");
        GetComponent<Renderer>().material.mainTexture = tex;
        Debug.Log("set Main Texture done.");


        //showUsedMemorySize();
    }
}
