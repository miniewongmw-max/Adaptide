using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaPanel : MonoBehaviour
{
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private void Awake() => Apply();

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height)
            Apply();
    }

    private void Apply()
    {
        Rect safe = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;

        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = safe.position / new Vector2(Screen.width, Screen.height);
        rect.anchorMax = (safe.position + safe.size) / new Vector2(Screen.width, Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        lastSafeArea = safe;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
    }
}
