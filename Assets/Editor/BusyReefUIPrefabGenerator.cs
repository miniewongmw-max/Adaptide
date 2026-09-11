using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BusyReefUIPrefabGenerator
{
    private const string Folder = "Assets/Resources/UI";

    [MenuItem("Busy Reef/UI/Create Editable UI Prefabs")]
    public static void Generate()
    {
        EnsureFolder("Assets/Resources", "UI");
        CreateCanvasPrefab();
        CreatePanelPrefab();
        CreateTextPrefab();
        CreateButtonPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Busy Reef editable UI prefabs are ready in Assets/Resources/UI.");
    }

    private static void CreateCanvasPrefab()
    {
        string path = Folder + "/BusyReefCanvas.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = new GameObject("BusyReefCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GameObject safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaPanel));
        safe.transform.SetParent(root.transform, false);
        Stretch(safe.GetComponent<RectTransform>());
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreatePanelPrefab()
    {
        string path = Folder + "/BusyReefPanel.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = new GameObject("BusyReefPanel", typeof(RectTransform), typeof(Image));
        Image image = root.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateTextPrefab()
    {
        string path = Folder + "/BusyReefText.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = new GameObject("BusyReefText", typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
        text.text = "UI TEXT";
        text.fontSize = 40f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateButtonPrefab()
    {
        string path = Folder + "/BusyReefButton.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        GameObject root = new GameObject("BusyReefButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Image image = root.GetComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        Stretch(labelObject.GetComponent<RectTransform>(), 12f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "BUTTON";
        label.fontSize = 40f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
