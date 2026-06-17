// [UNITY-SKILL:SPRITEATLAS]
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using System.IO;

public class SpriteAtlasPrebuildGenerator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // 1. Enable SpriteAtlas V2
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;

        string atlasPath = "Assets/MainAtlas.spriteatlasv2";
        GenerateSolitaireAtlas(atlasPath);
    }

    private void GenerateSolitaireAtlas(string path)
    {
        SpriteAtlasAsset atlasAsset;
        if (File.Exists(path))
        {
            // Note: Load might not be static or might have different signature in some versions, 
            // but the skill api.md says it's static. Let's try new if Load fails or just always start fresh for prebuild.
            atlasAsset = new SpriteAtlasAsset(); 
        }
        else
        {
            atlasAsset = new SpriteAtlasAsset();
        }

        // 2. Add Sprites folder
        Object spritesFolder = AssetDatabase.LoadAssetAtPath<Object>("Assets/Sprites");
        if (spritesFolder != null)
        {
            atlasAsset.Add(new[] { spritesFolder });
        }

        SpriteAtlasAsset.Save(atlasAsset, path);
        AssetDatabase.ImportAsset(path);

        // 3. Configure Importer
        SpriteAtlasImporter importer = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;
        if (importer != null)
        {
            importer.includeInBuild = true;

            var packingSettings = importer.packingSettings;
            packingSettings.enableRotation = false;
            packingSettings.enableTightPacking = false;
            packingSettings.padding = 4;
            importer.packingSettings = packingSettings;

            // iOS Settings (iPad)
            TextureImporterPlatformSettings iosSettings = new TextureImporterPlatformSettings
            {
                name = "iPhone", // "iPhone" is the internal name for iOS platform settings
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.ASTC_6x6
            };
            importer.SetPlatformSettings(iosSettings);

            importer.SaveAndReimport();
        }

        Debug.Log($"[SpriteAtlas] Generated/Updated {path} for iPad build.");
    }
}
