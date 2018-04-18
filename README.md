# About

Sample unity3d project to demonstrate WebRTC official unity plugin.
with simple WebRTC signaling implemented with Socket.IO.

# Requirements

* Unity3D 2017.3+
* node.js

# Using Libraries

* WebRTC (official) unity plugin: https://github.com/mhama/webrtc-dev-env

* `Socket.IO` library: https://github.com/floatinghotpot/socket.io-unity

# Instructions

## launch signaling server

* On Windows:

```sh
> server.bat
```
or

* On Mac/Linux

```sh
npm install
node index.js
```

* Output Log

```
>call npm install
npm WARN webrtc-simple-signaling-server@0.0.1 No description
npm WARN webrtc-simple-signaling-server@0.0.1 No repository field.
...

listening on *:3000
forwarding to global domain...
ngrok server: https://440ab904.ngrok.io
```

* The last "ngrok server" part is IMPORTANT!
* the server URL lasts for 24 hours only. After that, you need to restart server for another 24 hours.

## Launch unity app

`build and run` unity project.

## Start WebRTC on unity app

* set the above `ngrok server` URL to app's server URL field.
* push `Connect Server` button
* push `Start WebRTC` button
* push `Start Camera` button

## Start the other side of WebRTC

* set the above `ngrok server` URL to web browser. (need to support WebRTC).
* push `Start Camera` button
* push `Start Peer` button

# Signaling

Using signaling with http long polling of Socket.IO.
Very dirty implementation that doesnot handle bad states.

# Caution

Beware that WebRTC use very much bandwidth!
