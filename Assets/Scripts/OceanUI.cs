using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OceanUI
{
    private const string CanvasPrefabPath = "UI/BusyReefCanvas";
    private const string PanelPrefabPath = "UI/BusyReefPanel";
    private const string TextPrefabPath = "UI/BusyReefText";
    private const string ButtonPrefabPath = "UI/BusyReefButton";

    public static readonly Color Deep = new Color32(3, 31, 56, 255);
    public static readonly Color Navy = new Color32(4, 52, 79, 245);
    public static readonly Color Panel = new Color32(7, 79, 103, 232);
    public static readonly Color Aqua = new Color32(73, 222, 205, 255);
    public static readonly Color Foam = new Color32(231, 255, 244, 255);
    public static readonly Color Sand = new Color32(255, 225, 156, 255);
    public static readonly Color Coral = new Color32(255, 126, 103, 255);
    public static readonly Color Muted = new Color32(155, 211, 211, 255);

    private static TMP_FontAsset creamyFont;
    private static Sprite roundedSprite;

    public static Canvas CreateCanvas(string name, bool disableExisting = true)
    {
        if (disableExisting)
        {
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>())
                canvas.gameObject.SetActive(false);
        }

        EnsureEventSystem();
        GameObject canvasPrefab = Resources.Load<GameObject>(CanvasPrefabPath);
        GameObject go = canvasPrefab != null
            ? UnityEngine.Object.Instantiate(canvasPrefab)
            : new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.name = name;
        if (go.GetComponent<Canvas>() == null) go.AddComponent<Canvas>();
        if (go.GetComponent<CanvasScaler>() == null) go.AddComponent<CanvasScaler>();
        if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        Canvas canvasResult = go.GetComponent<Canvas>();
        canvasResult.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasResult.sortingOrder = 50;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Transform existingSafe = go.transform.Find("SafeArea");
        GameObject safe = existingSafe != null ? existingSafe.gameObject : CreateObject("SafeArea", go.transform);
        Stretch(safe.GetComponent<RectTransform>(), 0f);
        if (safe.GetComponent<SafeAreaPanel>() == null) safe.AddComponent<SafeAreaPanel>();
        return canvasResult;
    }

    public static RectTransform SafeRoot(Canvas canvas)
    {
        Transform safe = canvas.transform.Find("SafeArea");
        return safe != null ? safe as RectTransform : canvas.transform.GetChild(0) as RectTransform;
    }

    public static GameObject CreateObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static Image CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panelPrefab = Resources.Load<GameObject>(PanelPrefabPath);
        GameObject go = panelPrefab != null
            ? UnityEngine.Object.Instantiate(panelPrefab, parent, false)
            : CreateObject(name, parent);
        go.name = name;
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.color = color;
        if (image.sprite == null)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    public static TMP_Text CreateText(string text, Transform parent, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textPrefab = Resources.Load<GameObject>(TextPrefabPath);
        GameObject go = textPrefab != null
            ? UnityEngine.Object.Instantiate(textPrefab, parent, false)
            : CreateObject("Text", parent);
        go.name = "Text";
        TMP_Text label = go.GetComponent<TMP_Text>();
        if (label == null) label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        TMP_FontAsset font = CreamyFont();
        if (font != null) label.font = font;
        Stretch(label.rectTransform, 12f);
        return label;
    }

    public static Button CreateButton(string name, string label, Transform parent, Color color, Action onClick)
    {
        GameObject buttonPrefab = Resources.Load<GameObject>(ButtonPrefabPath);
        GameObject go = buttonPrefab != null
            ? UnityEngine.Object.Instantiate(buttonPrefab, parent, false)
            : CreatePanel(name, parent, color).gameObject;
        go.name = name;
        Image image = go.GetComponent<Image>();
        if (image == null) image = go.AddComponent<Image>();
        image.color = color;
        if (image.sprite == null)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
        }
        Button button = go.GetComponent<Button>();
        if (button == null) button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.72f, 0.9f, 0.9f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.RemoveAllListeners();
        if (onClick != null) button.onClick.AddListener(() => onClick());
        TMP_Text buttonLabel = go.GetComponentInChildren<TMP_Text>(true);
        if (buttonLabel == null) buttonLabel = CreateText(label, image.transform, 40f, Deep);
        buttonLabel.text = label;
        buttonLabel.fontSize = 40f;
        buttonLabel.color = Deep;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.raycastTarget = false;
        TMP_FontAsset font = CreamyFont();
        if (font != null) buttonLabel.font = font;
        Stretch(buttonLabel.rectTransform, 12f);
        return button;
    }

    public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    public static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static void AddOceanBackground(Transform parent)
    {
        Image backdrop = CreatePanel("OceanBackdrop", parent, Deep);
        Stretch(backdrop.rectTransform, -8f);
        backdrop.transform.SetAsFirstSibling();

        Color[] colors =
        {
            new Color(0.10f, 0.70f, 0.72f, 0.10f),
            new Color(0.30f, 0.90f, 0.82f, 0.08f),
            new Color(1f, 0.75f, 0.45f, 0.06f)
        };
        for (int i = 0; i < 12; i++)
        {
            Image bubble = CreatePanel($"Bubble{i}", backdrop.transform, colors[i % colors.Length]);
            RectTransform rect = bubble.rectTransform;
            float x = ((i * 37) % 100) / 100f;
            float y = ((i * 61) % 100) / 100f;
            rect.anchorMin = rect.anchorMax = new Vector2(x, y);
            rect.sizeDelta = Vector2.one * (35f + (i % 4) * 22f);
        }
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    private static TMP_FontAsset CreamyFont()
    {
        if (creamyFont != null) return creamyFont;
        Font source = Resources.Load<Font>("Fonts/Creamy Chicken");
        if (source == null) return null;
        creamyFont = TMP_FontAsset.CreateFontAsset(source);
        creamyFont.name = "Creamy Chicken Runtime SDF";
        return creamyFont;
    }

    private static Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 32;
        const float radius = 9f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Busy Reef Rounded Panel",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float cx = x < radius ? radius : x > size - radius - 1 ? size - radius - 1 : x;
            float cy = y < radius ? radius : y > size - radius - 1 ? size - radius - 1 : y;
            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            byte alpha = distance <= radius ? (byte)255 : (byte)0;
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        roundedSprite.name = "Busy Reef Rounded Sprite";
        return roundedSprite;
    }
}
