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

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScene);
        AssetDatabase.SaveAssets();
        Debug.Log("BUSY_REEF_CANVAS_BAKED: editable HUD and bottom menu saved in Gameplay.unity");
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
