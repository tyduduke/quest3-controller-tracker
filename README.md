# Quest 3 Controller Tracker

Streams Meta Quest 3 controller data (pose, velocity, buttons) from a Unity VR app to a desktop Python receiver over UDP in real-time.

## Architecture

```
Quest 3 (Unity)                          Desktop (Python)
┌─────────────────────┐     UDP/JSON     ┌──────────────┐
│ ControllerTracker   │ ──────────────── │ receiver.py  │
│ NetworkSender       │    port 7000     │              │
│ IPAddressUI         │                  └──────────────┘
└─────────────────────┘
```

**Unity side** — Polls left/right XR controllers at 15 Hz, serializes pose + button state to JSON, sends over UDP.

**Desktop side** — Standalone Python script that listens for UDP packets, pretty-prints controller state, and optionally saves to JSONL.

## Unity Setup

1. Open your Unity project (requires TextMeshPro package).
2. Copy the C# scripts into your Assets folder.
3. Use the menu **Quest3Tracker > Build Scene** to auto-create the full GameObject hierarchy.
4. Add an XR Origin / OVRCameraRig to the scene.
5. Set the Canvas Event Camera to the XR Main Camera.
6. Build and deploy to Quest 3.

## Receiver Usage

```bash
python receiver.py                     # listen on 0.0.0.0:7000
python receiver.py --port 8000         # custom port
python receiver.py --save log.jsonl    # also save to JSONL file
```

No external dependencies — uses only the Python standard library.

## How It Works

1. On launch, a world-space VR panel prompts for the receiver's IP address and port.
2. After pressing Connect, the UI hides and controller tracking begins.
3. Each frame tick (at the configured frequency), a JSON snapshot is sent over UDP containing:
   - Timestamp (ISO-8601 UTC) and Unity time
   - Per-hand: position, rotation (quaternion), velocity, angular velocity
   - Per-hand buttons: trigger, grip, primary (X/A), secondary (Y/B), thumbstick, menu

## Configuration

| Setting        | Default          | Location                  |
|----------------|------------------|---------------------------|
| Send frequency | 15 Hz            | `ControllerTracker.sendFrequency` |
| UDP port       | 7000             | `NetworkSender.port` / receiver `--port` |
| Target IP      | 192.168.1.100    | `IPAddressUI.defaultIP`   |
