using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MontAR.EditorTools
{
    /// <summary>
    /// Cria os assets de fonte do TextMeshPro a partir da Plus Jakarta Sans (Assets/Fonts, licença OFL):
    /// Regular, com SemiBold, Bold e ExtraBold ligados na tabela de pesos (FontStyles.Bold usa a Bold;
    /// fontWeight = Heavy usa a ExtraBold). Atlas dinâmico, já com os caracteres do português.
    /// A LiberationSans fica de reserva para algum símbolo que a fonte não tenha.
    /// </summary>
    public static class FontBuilder
    {
        public const string Folder = "Assets/Fonts/PlusJakartaSans/";
        public const string RegularAssetPath = Folder + "PlusJakartaSans SDF.asset";
        private const string FallbackPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~"
            + "ÀÁÂÃÇÉÊÍÓÔÕÚÜàáâãçéêíóôõúü·•–—‹›×°…ºª";

        private static readonly (string file, string asset, int weight)[] Weights =
        {
            ("PlusJakartaSans-SemiBold.ttf", "PlusJakartaSans SemiBold SDF.asset", 6),
            ("PlusJakartaSans-Bold.ttf", "PlusJakartaSans Bold SDF.asset", 7),
            ("PlusJakartaSans-ExtraBold.ttf", "PlusJakartaSans ExtraBold SDF.asset", 8),
        };

        /// <summary>Cria o que faltar e devolve a fonte Regular (com os pesos ligados). Null se a fonte não estiver no projeto.</summary>
        public static TMP_FontAsset EnsureFonts()
        {
            TMP_FontAsset regular = EnsureAsset("PlusJakartaSans-Regular.ttf", RegularAssetPath);
            if (regular == null)
                return null;

            foreach ((string file, string asset, int weight) in Weights)
            {
                TMP_FontAsset typeface = EnsureAsset(file, Folder + asset);
                if (typeface != null)
                    regular.fontWeightTable[weight].regularTypeface = typeface;
            }

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
            foreach (TMP_FontAsset font in AllAssets(regular))
            {
                if (font.fallbackFontAssetTable == null)
                    font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                if (fallback != null && !font.fallbackFontAssetTable.Contains(fallback))
                    font.fallbackFontAssetTable.Add(fallback);
                EditorUtility.SetDirty(font);
            }
            AssetDatabase.SaveAssets();
            return regular;
        }

        private static TMP_FontAsset[] AllAssets(TMP_FontAsset regular)
        {
            var fonts = new TMP_FontAsset[Weights.Length + 1];
            fonts[0] = regular;
            for (int i = 0; i < Weights.Length; i++)
                fonts[i + 1] = regular.fontWeightTable[Weights[i].weight].regularTypeface;
            return System.Array.FindAll(fonts, f => f != null);
        }

        private static TMP_FontAsset EnsureAsset(string fontFile, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + fontFile);
            if (source == null)
            {
                Debug.LogWarning($"[MontAR] Fonte não encontrada: {Folder}{fontFile}. A interface usa a fonte padrão do TextMeshPro.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 60, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters(Characters, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.Log($"[MontAR] {asset.name}: sem os caracteres '{missing}' (vêm da fonte reserva).");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
