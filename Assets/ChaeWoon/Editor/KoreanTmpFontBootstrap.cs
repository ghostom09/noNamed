using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class KoreanTmpFontBootstrap
{
    private const string SourceFontPath = "Assets/ChaeWoon/Fonts/NotoSansKR-Regular.otf";
    private const string FontAssetPath = "Assets/ChaeWoon/Fonts/NotoSansKR-Regular SDF.asset";
    private const string PrefabRoot = "Assets/ChaeWoon/Prefabs";
    private const string ReplaceTargetFontPrefix = "LiberationSans";

    [InitializeOnLoadMethod]
    private static void RunOnLoad()
    {
        EditorApplication.delayCall += () => EnsureKoreanFont(false);
    }

    [MenuItem("Tools/ChaeWoon/Rebuild Korean TMP Font")]
    private static void RunFromMenu()
    {
        EnsureKoreanFont(true);
    }

    private static void EnsureKoreanFont(bool force)
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

        if (fontAsset == null || force)
        {
            fontAsset = CreateFontAsset();

            if (fontAsset == null)
            {
                return;
            }
        }

        ApplyToPrefabs(fontAsset);
    }

    private static TMP_FontAsset CreateFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);

        if (sourceFont == null)
        {
            Debug.LogWarning($"[KoreanTmpFontBootstrap] 소스 폰트가 없습니다: {SourceFontPath}");
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(FontAssetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont, 60, 6, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);

        if (fontAsset == null)
        {
            Debug.LogWarning("[KoreanTmpFontBootstrap] TMP 폰트 에셋 생성에 실패했습니다.");
            return null;
        }

        fontAsset.name = Path.GetFileNameWithoutExtension(FontAssetPath);
        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
        {
            fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[KoreanTmpFontBootstrap] 한글 TMP 폰트 에셋 생성: {FontAssetPath}");
        return fontAsset;
    }

    private static void ApplyToPrefabs(TMP_FontAsset fontAsset)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == fontAsset)
                {
                    continue;
                }

                if (text.font == null || text.font.name.StartsWith(ReplaceTargetFontPrefix))
                {
                    text.font = fontAsset;
                    changed = true;
                }
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[KoreanTmpFontBootstrap] 프리팹 폰트 교체: {path}");
            }

            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
