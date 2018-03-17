using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectButton : MonoBehaviour {

    public InputField urlInput;
    public WebRTCNaticeCallSample webrtc;

	// Use this for initialization
	void Start () {
        string serverUrl = PlayerPrefs.GetString("serverUrl");
        if (serverUrl != null)
        {
            urlInput.text = serverUrl;
        }
        if (urlInput.text == null || urlInput.text == "")
        {
            urlInput.text = webrtc.serverURL;
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ButtonPushed()
    {
        string serverUrl = urlInput.text;
        webrtc.serverURL = serverUrl;
        webrtc.ConnectToServer();
        PlayerPrefs.SetString("serverUrl", serverUrl);
    }
}
