using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BusyReefCanvasBaker
{
    private const string GameplayScene = "Assets/Scenes/Gameplay.unity";

    [MenuItem("Tools/Busy Reef/Bake Editable Gameplay Canvas")]
    public static void BakeGameplayScene()
    {
        EnsureCreamyFontAsset();
        BusyReefRuntimeSetup.EnsureRoundedSpriteAsset();
        Scene scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        RemoveDuplicateEventSystems();
        DestroyCanvas("Busy Reef Gameplay HUD");
        DestroyCanvas("Busy Reef Bottom Hub");

        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        if (manager == null) throw new MissingComponentException("Gameplay scene needs a GameManager before UI can be baked.");

        manager.BuildGameplayUIForEditor();
        MainMenuBehaviour builder = manager.gameObject.AddComponent<MainMenuBehaviour>();
        builder.BuildHubForEditor(true);
        Object.DestroyImmediate(builder);
        BusyReefRuntimeSetup.EnsureAudioObject();
        ConfigureCanvasPreviews();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScene);
        AssetDatabase.SaveAssets();
        Debug.Log("BUSY_REEF_CANVAS_BAKED: editable HUD and bottom menu saved in Gameplay.unity");
    }

    [MenuItem("Tools/Busy Reef/Refresh Canvas Inspector Preview")]
    public static void RefreshCanvasInspectorPreview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != GameplayScene)
        {
            Debug.LogWarning("Open Assets/Scenes/Gameplay.unity before refreshing its Canvas preview.");
            return;
        }

        ConfigureCanvasPreviews();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("BUSY_REEF_CANVAS_PREVIEW_READY: choose Portrait/Landscape and the visible page in each Canvas inspector.");
    }

    [InitializeOnLoadMethod]
    private static void RefreshOpenGameplaySceneAfterCompile()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += RefreshPreviewIfNeeded;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == GameplayScene) EditorApplication.delayCall += RefreshPreviewIfNeeded;
    }

    private static void RefreshPreviewIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != GameplayScene) return;

        bool missing = false;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if ((canvas.name == "Busy Reef Bottom Hub" || canvas.name == "Busy Reef Gameplay HUD") &&
                canvas.GetComponent<BusyReefCanvasPreview>() == null)
            {
                missing = true;
                break;
            }
        }
        if (!missing) return;

        ConfigureCanvasPreviews();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("BUSY_REEF_CANVAS_PREVIEW_ATTACHED: Canvas controls are now visible in the Inspector.");
    }

    private static void ConfigureCanvasPreviews()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            BusyReefCanvasPreview preview = canvas.GetComponent<BusyReefCanvasPreview>();
            if (preview == null) preview = canvas.gameObject.AddComponent<BusyReefCanvasPreview>();
            if (canvas.name == "Busy Reef Bottom Hub")
            {
                preview.canvasKind = BusyReefCanvasPreview.CanvasKind.BottomHub;
                preview.previewOrientation = BusyReefCanvasPreview.PreviewOrientation.Portrait;
                preview.previewHubPage = BusyReefCanvasPreview.HubPage.Play;
            }
            else if (canvas.name == "Busy Reef Gameplay HUD")
            {
                preview.canvasKind = BusyReefCanvasPreview.CanvasKind.GameplayHud;
                preview.previewHudState = BusyReefCanvasPreview.HudState.Ready;
            }
            else continue;
            preview.ApplyPreview();
            EditorUtility.SetDirty(preview);
        }
    }

    private static void DestroyCanvas(string canvasName)
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            if (canvas.name == canvasName) Object.DestroyImmediate(canvas.gameObject);
    }

    private static void RemoveDuplicateEventSystems()
    {
        UnityEngine.EventSystems.EventSystem[] systems =
            Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        for (int i = 1; i < systems.Length; i++) Object.DestroyImmediate(systems[i].gameObject);
    }

    private static void EnsureCreamyFontAsset()
    {
        const string assetPath = "Assets/Resources/Fonts/Creamy Chicken SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(assetPath) != null) return;
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Creamy Chicken.otf");
        if (source == null) throw new MissingReferenceException("Creamy Chicken.otf is missing from Resources/Fonts.");
        TMPro.TMP_FontAsset fontAsset = TMPro.TMP_FontAsset.CreateFontAsset(source);
        fontAsset.name = "Creamy Chicken SDF";
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        if (fontAsset.atlasTexture != null) AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        if (fontAsset.material != null) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
    }
}
