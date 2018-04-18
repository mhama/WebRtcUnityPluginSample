using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusLabel : MonoBehaviour {

    public WebRtcNativeCallSample webrtc;
    Text text;

	// Use this for initialization
	void Start () {
        text = GetComponent<Text>();

    }
	
	// Update is called once per frame
	void Update () {
        var status = "Status: " + webrtc.status;
        if (text.text != status)
        {
            text.text = status;
        }
	}
}
