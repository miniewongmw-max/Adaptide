using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BusyReefRuntimeSetup
{
    private const string GameplayScene = "Assets/Scenes/Gameplay.unity";

    [MenuItem("Tools/Busy Reef/Ensure Audio And Shadow Setup")]
    public static void SetupGameplayScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
        EnsureAudioObject();
        EnsureSceneShadows();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, GameplayScene);
        AssetDatabase.SaveAssets();
        Debug.Log("BUSY_REEF_RUNTIME_SETUP_COMPLETE");
    }

    public static void EnsureAudioObject()
    {
        GameAudioManager manager = Object.FindAnyObjectByType<GameAudioManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            GameObject audioObject = new GameObject("Game Audio");
            manager = audioObject.AddComponent<GameAudioManager>();
        }

        AudioSource[] sources = manager.GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            while (sources.Length < 2)
            {
                manager.gameObject.AddComponent<AudioSource>();
                sources = manager.GetComponents<AudioSource>();
            }
        }

        manager.musicSource = sources[0];
        manager.musicSource.playOnAwake = false;
        manager.musicSource.loop = true;
        manager.sfxSource = sources[1];
        manager.sfxSource.playOnAwake = false;
        manager.sfxSource.loop = false;
        EditorUtility.SetDirty(manager);
    }

    public static void EnsureRoundedSpriteAsset()
    {
        const string assetPath = "Assets/Resources/UI/BusyReefRounded.png";
        if (!File.Exists(assetPath))
        {
            const int size = 64;
            const float radius = 29f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = x < radius ? radius : x > size - radius - 1 ? size - radius - 1 : x;
                float cy = y < radius ? radius : y > size - radius - 1 ? size - radius - 1 : y;
                byte alpha = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= radius ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = new Vector4(29f, 29f, 29f, 29f);
        importer.SaveAndReimport();
    }

    private static void EnsureSceneShadows()
    {
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            if (light.type == LightType.Directional) light.shadows = LightShadows.Hard;

        foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }
}
