using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Tracks both Quest 3 controllers (pose + buttons) at a configurable frequency
/// and dispatches JSON payloads to the NetworkSender.
/// </summary>
public class ControllerTracker : MonoBehaviour
{
    [Header("Tracking Settings")]
    [Tooltip("How many JSON snapshots to send per second")]
    public float sendFrequency = 15f;

    private float _sendInterval;
    private float _timer;

    private InputDevice _leftController;
    private InputDevice _rightController;
    private bool _devicesFound;

    private NetworkSender _sender;
    private bool _trackingActive;

    // -------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------

    private void Awake()
    {
        _sender = GetComponent<NetworkSender>();
        if (_sender == null)
            _sender = gameObject.AddComponent<NetworkSender>();
    }

    private void Start()
    {
        _sendInterval = 1f / sendFrequency;
        _timer = 0f;
        _trackingActive = false;
    }

    private void Update()
    {
        if (!_trackingActive) return;

        // Lazy‑find controllers (they may not be ready on the first frame)
        if (!_devicesFound)
            TryFindControllers();

        _timer += Time.deltaTime;
        if (_timer >= _sendInterval)
        {
            _timer -= _sendInterval;
            CaptureAndSend();
        }
    }

    // -------------------------------------------------------------------
    // Public API (called by IPAddressUI after the user confirms the IP)
    // -------------------------------------------------------------------

    public void StartTracking()
    {
        _trackingActive = true;
        Debug.Log("[ControllerTracker] Tracking started.");
    }

    public void StopTracking()
    {
        _trackingActive = false;
        Debug.Log("[ControllerTracker] Tracking stopped.");
    }

    // -------------------------------------------------------------------
    // Device discovery
    // -------------------------------------------------------------------

    private void TryFindControllers()
    {
        var leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftDevices);

        var rightDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightDevices);

        if (leftDevices.Count > 0) _leftController = leftDevices[0];
        if (rightDevices.Count > 0) _rightController = rightDevices[0];

        _devicesFound = leftDevices.Count > 0 && rightDevices.Count > 0;
    }

    // -------------------------------------------------------------------
    // Snapshot capture
    // -------------------------------------------------------------------

    private void CaptureAndSend()
    {
        ControllerSnapshot snapshot = new ControllerSnapshot
        {
            timestamp = DateTime.UtcNow.ToString("o"),          // ISO‑8601
            unityTime = Time.time,
            left = CaptureController(_leftController, "left"),
            right = CaptureController(_rightController, "right")
        };

        string json = JsonUtility.ToJson(snapshot, false);
        _sender.Send(json);
    }

    private ControllerData CaptureController(InputDevice device, string hand)
    {
        ControllerData data = new ControllerData();
        data.hand = hand;
        data.isValid = device.isValid;

        if (!device.isValid) return data;

        // ---- Pose ----
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
        {
            data.posX = pos.x;
            data.posY = pos.y;
            data.posZ = pos.z;
        }

        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            data.rotX = rot.x;
            data.rotY = rot.y;
            data.rotZ = rot.z;
            data.rotW = rot.w;
        }

        if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 vel))
        {
            data.velX = vel.x;
            data.velY = vel.y;
            data.velZ = vel.z;
        }

        if (device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 angVel))
        {
            data.angVelX = angVel.x;
            data.angVelY = angVel.y;
            data.angVelZ = angVel.z;
        }

        // ---- Buttons ----
        device.TryGetFeatureValue(CommonUsages.triggerButton, out data.triggerButton);
        device.TryGetFeatureValue(CommonUsages.trigger, out data.triggerValue);

        device.TryGetFeatureValue(CommonUsages.gripButton, out data.gripButton);
        device.TryGetFeatureValue(CommonUsages.grip, out data.gripValue);

        device.TryGetFeatureValue(CommonUsages.primaryButton, out data.primaryButton);       // X / A
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out data.secondaryButton);   // Y / B

        device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out data.thumbstickClick);
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
        data.thumbstickX = thumbstick.x;
        data.thumbstickY = thumbstick.y;

        device.TryGetFeatureValue(CommonUsages.menuButton, out data.menuButton);

        return data;
    }
}

// ======================================================================
// Serialisable data structures  (JsonUtility‑friendly — no dictionaries)
// ======================================================================

[Serializable]
public class ControllerSnapshot
{
    public string timestamp;   // ISO‑8601 UTC
    public float unityTime;    // Time.time
    public ControllerData left;
    public ControllerData right;
}

[Serializable]
public class ControllerData
{
    public string hand;
    public bool isValid;

    // Position
    public float posX, posY, posZ;

    // Rotation (quaternion)
    public float rotX, rotY, rotZ, rotW;

    // Velocity
    public float velX, velY, velZ;

    // Angular velocity
    public float angVelX, angVelY, angVelZ;

    // Buttons
    public bool triggerButton;
    public float triggerValue;
    public bool gripButton;
    public float gripValue;
    public bool primaryButton;     // X on left, A on right
    public bool secondaryButton;   // Y on left, B on right
    public bool thumbstickClick;
    public float thumbstickX;
    public float thumbstickY;
    public bool menuButton;
}
