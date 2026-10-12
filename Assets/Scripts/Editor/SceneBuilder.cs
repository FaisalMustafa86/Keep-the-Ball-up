using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Keep the Ball Up > Build Scene: puts every object the game uses (arena, paddle, ball, UI, effects, audio)
// into the open scene, so it can be seen and edited without pressing Play.
// It runs the same code the game runs when it starts, and only adds what's missing, so it's safe to run again.
public static class SceneBuilder
{
    // Old stand-in objects that the GameManager's own objects replace
    private static readonly string[] OldPlaceholders = { "Background", "player" };

    [MenuItem("Tools/Keep the Ball Up/Build Scene")]
    private static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before building the scene.");
            return;
        }

        SaveSprites();
        SpriteFactory.ClearCache();

        GameManager game = Object.FindFirstObjectByType<GameManager>();
        if (game == null)
        {
            game = new GameObject("GameManager").AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(game.gameObject, "Build Scene");
        }

        foreach (string name in OldPlaceholders)
        {
            GameObject old = GameObject.Find(name);
            if (old != null && old.transform.parent == null) Undo.DestroyObjectImmediate(old);
        }

        Vector2 gameView = Handles.GetMainGameViewSize();
        game.Build(new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(gameView.x)), Mathf.Max(1, Mathf.RoundToInt(gameView.y))));

        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        SaveScenes();
        Selection.activeGameObject = game.gameObject;
        Debug.Log("Keep the Ball Up: the game's objects are now in the scene under GameManager, and the scene is saved.");
    }

    // Saves the open scene straight to its file, without a dialog. The builds call this first, because Unity
    // otherwise asks to save modified scenes in a pop-up that can open hidden behind the editor.
    // A scene with no file yet (e.g. restored after a crash) is saved as the game's scene.
    [MenuItem("Tools/Keep the Ball Up/Save Scene")]
    public static void SaveScenes()
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || (!scene.isDirty && !string.IsNullOrEmpty(scene.path))) continue;

            string path = string.IsNullOrEmpty(scene.path) ? GameScenePath : scene.path;
            if (EditorSceneManager.SaveScene(scene, path)) Debug.Log("Saved scene: " + path);
            else Debug.LogError("Couldn't save scene to " + path);
        }
    }

    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";

    // Saves each shape SpriteFactory draws as a PNG in Assets/Sprites, set up as a sprite.
    // Files that already exist are left alone, so they can be edited.
    private static void SaveSprites()
    {
        Directory.CreateDirectory(SpriteFactory.AssetFolder);

        foreach (SpriteFactory.Shape shape in System.Enum.GetValues(typeof(SpriteFactory.Shape)))
        {
            string path = SpriteFactory.AssetFolder + shape + ".png";
            if (File.Exists(path)) continue;

            Texture2D tex = SpriteFactory.Draw(shape);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpriteFactory.PixelsPerUnit(shape);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteBorder = SpriteFactory.Border(shape);
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
