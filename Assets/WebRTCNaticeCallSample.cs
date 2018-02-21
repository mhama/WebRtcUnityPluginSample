using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePeerConnectionM;

public class WebRTCNaticeCallSample : MonoBehaviour {

	// Use this for initialization
	void Start () {
#if UNITY_ANDROID

        AndroidJavaClass playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        AndroidJavaObject activity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaClass webrtcClass = new AndroidJavaClass("org.webrtc.PeerConnectionFactory");

        if (webrtcClass != null)

        {

            webrtcClass.CallStatic("initializeAndroidGlobals", new object[2] { activity, false });

        }

#endif
        PeerConnectionM peer = new PeerConnectionM(new List<string>(), "user", "cred");
        int id = peer.GetUniqueId();
        Debug.Log("PeerConnectionM.GetUniqueId() : " + id);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
