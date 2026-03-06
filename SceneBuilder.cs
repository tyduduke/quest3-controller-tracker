using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor utility: creates the complete scene hierarchy in one click.
/// Menu → Quest3Tracker → Build Scene
/// </summary>
public class SceneBuilder : MonoBehaviour
{
    [MenuItem("Quest3Tracker/Build Scene")]
    public static void BuildScene()
    {
        // ── Root GameObject ────────────────────────────────────────────
        GameObject root = new GameObject("ControllerTrackerSystem");
        var tracker = root.AddComponent<ControllerTracker>();
        var sender  = root.AddComponent<NetworkSender>();
        var ui      = root.AddComponent<IPAddressUI>();

        // ── World-Space Canvas ─────────────────────────────────────────
        GameObject canvasObj = new GameObject("IPAddressCanvas");
        canvasObj.transform.SetParent(root.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600, 400);
        canvasRect.localScale = Vector3.one * 0.002f;   // 1.2m × 0.8m in world
        canvasRect.localPosition = new Vector3(0f, 1.3f, 1.5f); // ~eye height, 1.5m forward

        // ── Background Panel ───────────────────────────────────────────
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── Title ──────────────────────────────────────────────────────
        CreateLabel(panel.transform, "TitleLabel",
            "Controller Tracker", 32, FontStyles.Bold,
            new Vector2(0, 150), new Vector2(500, 50));

        // ── Status text ────────────────────────────────────────────────
        var statusGO = CreateLabel(panel.transform, "StatusText",
            "Enter receiver IP and port, then press Connect.", 18, FontStyles.Italic,
            new Vector2(0, 100), new Vector2(500, 40));
        ui.statusText = statusGO.GetComponent<TextMeshProUGUI>();

        // ── IP Input ───────────────────────────────────────────────────
        CreateLabel(panel.transform, "IPLabel", "IP Address:", 20, FontStyles.Normal,
            new Vector2(-110, 40), new Vector2(150, 35));
        GameObject ipField = CreateInputField(panel.transform, "IPInput",
            "192.168.1.100", new Vector2(80, 40), new Vector2(260, 45));
        ui.ipInputField = ipField.GetComponent<TMP_InputField>();

        // ── Port Input ─────────────────────────────────────────────────
        CreateLabel(panel.transform, "PortLabel", "Port:", 20, FontStyles.Normal,
            new Vector2(-110, -20), new Vector2(150, 35));
        GameObject portField = CreateInputField(panel.transform, "PortInput",
            "7000", new Vector2(80, -20), new Vector2(260, 45));
        ui.portInputField = portField.GetComponent<TMP_InputField>();

        // ── Connect Button ─────────────────────────────────────────────
        GameObject btnObj = new GameObject("ConnectButton");
        btnObj.transform.SetParent(panel.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f, 1f);
        Button btn = btnObj.AddComponent<Button>();
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -90);
        btnRect.sizeDelta = new Vector2(240, 55);
        ui.connectButton = btn;

        var btnLabel = CreateLabel(btnObj.transform, "BtnLabel", "CONNECT", 24, FontStyles.Bold,
            Vector2.zero, new Vector2(240, 55));

        // ── Wire up canvas reference ───────────────────────────────────
        ui.uiCanvas = canvas;

        Selection.activeGameObject = root;
        Debug.Log("[SceneBuilder] Scene hierarchy created. " +
                  "Add an XR Origin / OVRCameraRig and set the Canvas Event Camera.");
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static GameObject CreateLabel(Transform parent, string name, string text,
        float fontSize, FontStyles style, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    private static GameObject CreateInputField(Transform parent, string name,
        string placeholder, Vector2 anchoredPos, Vector2 size)
    {
        // Outer container
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(go.transform, false);
        RectTransform taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 5);
        taRect.offsetMax = new Vector2(-10, -5);

        // Placeholder
        GameObject phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = placeholder;
        phTMP.fontSize = 20;
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
        phTMP.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform phRT = phGO.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;

        // Editable text
        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI txtTMP = txtGO.AddComponent<TextMeshProUGUI>();
        txtTMP.text = "";
        txtTMP.fontSize = 20;
        txtTMP.color = Color.white;
        txtTMP.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        // TMP_InputField component
        TMP_InputField input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = txtTMP;
        input.placeholder = phTMP;
        input.text = placeholder;
        input.fontAsset = txtTMP.font;

        return go;
    }
}
#endif
