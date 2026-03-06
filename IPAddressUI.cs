using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a floating VR panel at startup where the user enters the target IP address.
/// After confirming, hides everything and starts controller tracking.
///
/// SETUP (in Unity Editor):
///   1. Create a World Space Canvas (render mode = World Space).
///   2. Add a Panel with: TMP_InputField (IP), TMP_InputField (Port), Button (Connect).
///   3. Drag references into this component's inspector fields.
///   4. Attach this script + ControllerTracker + NetworkSender to a single GameObject.
///   5. Set the Canvas's Event Camera to the XR Main Camera.
/// </summary>
public class IPAddressUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas uiCanvas;
    public TMP_InputField ipInputField;
    public TMP_InputField portInputField;
    public Button connectButton;
    public TextMeshProUGUI statusText;

    [Header("Defaults")]
    public string defaultIP = "192.168.1.100";
    public int defaultPort = 7000;

    private ControllerTracker _tracker;
    private NetworkSender _sender;

    private void Awake()
    {
        _tracker = GetComponent<ControllerTracker>();
        _sender = GetComponent<NetworkSender>();
    }

    private void Start()
    {
        // Pre‑fill defaults
        if (ipInputField != null)
            ipInputField.text = defaultIP;
        if (portInputField != null)
            portInputField.text = defaultPort.ToString();

        // Hook up button
        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);

        if (statusText != null)
            statusText.text = "Enter the receiver IP address and port, then press Connect.";
    }

    public void OnConnectClicked()
    {
        string ip = ipInputField != null ? ipInputField.text.Trim() : defaultIP;
        string portStr = portInputField != null ? portInputField.text.Trim() : defaultPort.ToString();

        // ---- Validate IP ----
        if (!System.Net.IPAddress.TryParse(ip, out _))
        {
            if (statusText != null)
                statusText.text = $"<color=red>Invalid IP address: {ip}</color>";
            return;
        }

        // ---- Validate port ----
        if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
        {
            if (statusText != null)
                statusText.text = $"<color=red>Invalid port: {portStr}</color>";
            return;
        }

        // ---- Configure sender ----
        _sender.Configure(ip, port);

        if (statusText != null)
            statusText.text = $"<color=green>Connected → {ip}:{port}</color>";

        // Hide the UI after a brief flash so the user sees confirmation
        Invoke(nameof(HideUIAndStartTracking), 0.8f);
    }

    private void HideUIAndStartTracking()
    {
        // Disable the entire canvas so the user sees only passthrough / black
        if (uiCanvas != null)
            uiCanvas.gameObject.SetActive(false);

        // Start the tracking loop
        _tracker.StartTracking();

        Debug.Log("[IPAddressUI] UI hidden. Tracking is live.");
    }
}
