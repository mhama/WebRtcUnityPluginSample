using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePeerConnectionM;

public class WebRTCNaticeCallSample : MonoBehaviour {

    //bool first = true;

    // Use this for initialization
    void Start() {
        Debug.Log("Sample.Start()");
        InitWebRTC();
    }

    void InitWebRTC() {
#if UNITY_ANDROID
        AndroidJavaClass systemClass = new AndroidJavaClass("java.lang.System");
        string libname = "jingle_peerconnection_so";
        systemClass.CallStatic("loadLibrary", new object[1] { libname });
        Debug.Log("loadLibrary loaded : "+ libname);

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
        AndroidJavaObject options = builder.Call< AndroidJavaObject>("createInitializationOptions");
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
	}
	
	// Update is called once per frame
	void Update () {
        /*
		if (first)
        {
            first = false;
            InitWebRTC();
        }
        */
	}
}
