using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor-only visibility helper for the baked Canvas hierarchy. It never
/// creates UI and does nothing while the game is running.
/// </summary>
[ExecuteAlways]
public class BusyReefCanvasPreview : MonoBehaviour
{
    public enum CanvasKind { GameplayHud, BottomHub }
    public enum PreviewOrientation { Portrait, Landscape }
    public enum HubPage { Mode, Shop, Play, Character, Settings }
    public enum HudState { Ready, Gameplay, Pause, Result, TutorialComplete }

    public CanvasKind canvasKind;
    [Tooltip("Changes only the Scene view preview. Runtime still follows the real device orientation.")]
    public PreviewOrientation previewOrientation = PreviewOrientation.Portrait;
    public HubPage previewHubPage = HubPage.Play;
    public HudState previewHudState = HudState.Ready;

    private void OnEnable()
    {
        if (!Application.isPlaying) ApplyPreview();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) ApplyPreview();
    }

    [ContextMenu("Apply Canvas Preview")]
    public void ApplyPreview()
    {
        if (Application.isPlaying) return;
        ApplyPreviewFrame();
        if (canvasKind == CanvasKind.BottomHub) ApplyHubPreview();
        else ApplyHudPreview();
    }

    private void ApplyPreviewFrame()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
            scaler.referenceResolution = previewOrientation == PreviewOrientation.Portrait
                ? new Vector2(1080f, 1920f)
                : new Vector2(1920f, 1080f);

        // The device safe area is applied at runtime. In the editor the full
        // preview must remain visible, even when the Game view aspect changes.
        RectTransform safeArea = FindComponent<RectTransform>("Safe Area");
        SetRect(safeArea, Vector2.zero, Vector2.one);
    }

    private void ApplyHubPreview()
    {
        ApplyHubOrientationPreview();

        string[] names =
        {
            "Mode Selection Page", "Shop Page", "Play Page",
            "Character Selection Page", "Settings Page"
        };
        int selected = (int)previewHubPage;
        for (int i = 0; i < names.Length; i++)
        {
            RectTransform page = FindComponent<RectTransform>(names[i]);
            if (page == null) continue;
            page.gameObject.SetActive(i == selected);
            if (i == selected) page.anchoredPosition = Vector2.zero;
        }

        Image backdrop = FindComponent<Image>("Full Screen Page Backdrop");
        if (backdrop != null)
        {
            bool show = previewHubPage != HubPage.Play;
            backdrop.gameObject.SetActive(show);
            if (show)
            {
                backdrop.color = previewHubPage switch
                {
                    HubPage.Mode => new Color32(155, 220, 240, 255),
                    HubPage.Shop => new Color32(174, 226, 242, 255),
                    HubPage.Character => new Color32(255, 169, 199, 255),
                    _ => new Color32(171, 226, 242, 255)
                };
            }
        }
    }

    private void ApplyHubOrientationPreview()
    {
        RectTransform edgeFill = FindComponent<RectTransform>("Navigation Edge Fill");
        RectTransform navigation = FindComponent<RectTransform>("Navigation Bar");
        RectTransform pages = FindComponent<RectTransform>("Sliding Pages");
        RectTransform backdrop = FindComponent<RectTransform>("Full Screen Page Backdrop");
        bool landscape = previewOrientation == PreviewOrientation.Landscape;

        if (landscape)
        {
            SetRect(edgeFill, new Vector2(0f, 0f), new Vector2(.14f, 1f));
            SetRect(backdrop, new Vector2(.14f, 0f), new Vector2(1f, 1f));
            SetRect(navigation, new Vector2(0f, .10f), new Vector2(.135f, .89f));
            SetRect(pages, new Vector2(.14f, .02f), new Vector2(.995f, .91f));
        }
        else
        {
            SetRect(edgeFill, new Vector2(0f, 0f), new Vector2(1f, .145f));
            SetRect(backdrop, new Vector2(0f, .145f), new Vector2(1f, 1f));
            SetRect(navigation, new Vector2(0f, 0f), new Vector2(1f, .14f));
            SetRect(pages, new Vector2(0f, .145f), new Vector2(1f, .91f));
        }

        string[] tileNames = { "MODE", "SHOP", "PLAY", "CHARACTER", "SETTINGS" };
        for (int i = 0; i < tileNames.Length; i++)
        {
            RectTransform tile = FindComponent<RectTransform>(tileNames[i]);
            if (landscape)
            {
                float yMin = 1f - (i + 1) / 5f + .025f;
                float yMax = 1f - i / 5f - .025f;
                SetRect(tile, new Vector2(.07f, yMin), new Vector2(.93f, yMax));
            }
            else
            {
                float xMin = i / 5f + .008f;
                float xMax = (i + 1) / 5f - .008f;
                SetRect(tile, new Vector2(xMin, .10f), new Vector2(xMax, .94f));
            }
        }
    }

    private void ApplyHudPreview()
    {
        SetActive("Pearls", previewHudState == HudState.Gameplay);
        SetActive("Pause", previewHudState == HudState.Gameplay);
        SetActive("CURRENT PAUSED", previewHudState == HudState.Pause);
        SetActive("Dive Result", previewHudState == HudState.Result);
        SetActive("TUTORIAL COMPLETE", previewHudState == HudState.TutorialComplete);
        SetActive("Tutorial Objective", previewHudState == HudState.Gameplay);

        for (int i = 1; i <= 4; i++)
            SetActive("Equipped Power " + i, previewHudState == HudState.Gameplay);
    }

    private void SetActive(string objectName, bool active)
    {
        Transform item = FindDeepChild(transform, objectName);
        if (item != null) item.gameObject.SetActive(active);
    }

    private T FindComponent<T>(string objectName) where T : Component
    {
        return FindDeepChild(transform, objectName)?.GetComponent<T>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rect == null) return;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), objectName);
            if (found != null) return found;
        }
        return null;
    }
}
