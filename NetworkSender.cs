using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Sends UTF-8 JSON strings to a remote host over UDP.
/// UDP is chosen for low-latency, fire-and-forget delivery at 15 Hz.
/// </summary>
public class NetworkSender : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 7000;

    private UdpClient _udpClient;
    private IPEndPoint _endPoint;
    private bool _isConfigured;

    // Stats (visible in Inspector for debugging)
    [Header("Runtime Stats (read-only)")]
    public int packetsSent;
    public int errors;

    /// <summary>
    /// Call this once after the user enters the target IP address.
    /// </summary>
    public void Configure(string ipAddress, int port)
    {
        this.port = port;
        try
        {
            _endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            _udpClient = new UdpClient();
            _isConfigured = true;
            packetsSent = 0;
            errors = 0;
            Debug.Log($"[NetworkSender] Configured → {ipAddress}:{port}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkSender] Configuration failed: {ex.Message}");
            _isConfigured = false;
        }
    }

    /// <summary>
    /// Send a JSON string. Silently drops if not yet configured.
    /// </summary>
    public void Send(string json)
    {
        if (!_isConfigured || _udpClient == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            _udpClient.Send(data, data.Length, _endPoint);
            packetsSent++;
        }
        catch (SocketException ex)
        {
            errors++;
            if (errors % 100 == 1)  // throttle log spam
                Debug.LogWarning($"[NetworkSender] Socket error #{errors}: {ex.Message}");
        }
        catch (Exception ex)
        {
            errors++;
            Debug.LogError($"[NetworkSender] Send error: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        Debug.Log($"[NetworkSender] Closed. Sent {packetsSent} packets, {errors} errors.");
    }
}
