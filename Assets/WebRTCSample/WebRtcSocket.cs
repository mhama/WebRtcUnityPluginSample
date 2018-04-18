using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quobject.SocketIoClientDotNet.Client;
using Newtonsoft.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Quobject.Collections.Immutable;

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

        socket.On(Socket.EVENT_CONNECT, () =>
        {
            Debug.Log("SocketIO: connected!");
            this.socket = socket;
            InitHandlers();
            this.OnConnect();
        });
        socket.On(Socket.EVENT_CONNECT_ERROR, (msg) =>
        {
            Debug.Log("SocketIO: connect error!");
            this.OnConnectError("error:"+msg);
        });
        socket.On(Socket.EVENT_DISCONNECT, () =>
        {
            Debug.Log("SocketIO: disconnected!");
            this.OnDisconnect();
            this.socket = null;
        });
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
        Debug.Log("SocketIO: Emit msgType:" + msgType + " {id:" + myUserId + ", msg:" + body + "}");
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
    public delegate void ErrorListener(string msg);

    public event MessageListener OnOffer;
    public event MessageListener OnAnswer;
    public event MessageListener OnJoin;
    public event MessageListener OnExit;
    public event MessageListener OnWelcome;
    public event ConnectionListener OnConnect;
    public event ErrorListener OnConnectError;
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