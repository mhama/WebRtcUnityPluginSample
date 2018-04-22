using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebRtcVideoPlayer : MonoBehaviour {

    private Texture2D tex;
    public FrameQueue frameQueue; // WebRtcNativeCallSampleがセットする。
    float lastUpdateTime;

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

        if (frameQueue != null) {
            _fpsLoad = frameQueue.Stats.fpsLoad();
            _fpsShow = frameQueue.Stats.fpsShow();
            _fpsSkip = frameQueue.Stats.fpsSkip();
        }
    }

    private void TryProcessFrame()
    {
        if (frameQueue != null)
        {
            FramePacket packet = frameQueue.Pop();
            //Debug.Log((packet == null ? "no frame to consume." : "frame consumed.") + "framesCount : " + frameQueue.Count);
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
        }
        //Debug.Log("Received Packet. " + packet.ToString());
        tex.LoadRawTextureData(packet.Buffer);
    
        tex.Apply();
        GetComponent<Renderer>().material.mainTexture = tex;
    }
}
