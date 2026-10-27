using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MontAR.Dev;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using static MontAR.EditorTools.UiFactory;
using Object = UnityEngine.Object;

namespace MontAR.EditorTools
{
    /// <summary>
    /// Monta o app na cena MontAR.unity: catálogo de peças, prefab da interface (apresentação,
    /// peças, início, montagem, conclusão), bancada virtual da demonstração 3D e as ligações entre
    /// os componentes. Rodar depois de MontAR › Prévia › Reconstruir cena de montagem.
    /// Atenção: reconstruir apaga ajustes feitos à mão no prefab MontAR_UI e na bancada virtual.
    /// O catálogo de peças só é criado se ainda não existir (edite-o no Inspector).
    /// </summary>
    public static class AppBuilder
    {
        public const string ScenePath = "Assets/Scenes/MontAR.unity";
        public const string UiPrefabPath = "Assets/Prefabs/UI/MontAR_UI.prefab";
        public const string CatalogPath = "Assets/Data/Pecas/Catalogo_pecas_v0.asset";
        public const string TmpEssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        public const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string UiRootName = "MontAR UI";
        public const string StageRootName = "Demonstração 3D";
        private const float StageDepth = -100f;

        // ------------------------------------------------------------ menus

        [MenuItem("MontAR/App/Reconstruir interface e cena")]
        private static void RebuildMenu()
        {
            if (!EnsureTmpEssentials())
            {
                Debug.Log("[MontAR] TMP Essential Resources importado. Rode o menu de novo depois da importação terminar.");
                return;
            }
            Debug.Log("[MontAR] " + RebuildApp());
        }

        [MenuItem("MontAR/App/Recriar catálogo de peças (sobrescreve)")]
        private static void RebuildCatalogMenu()
        {
            if (EditorUtility.DisplayDialog("Recriar catálogo", "Sobrescrever " + CatalogPath + " com o catálogo padrão?", "Sobrescrever", "Cancelar"))
                Debug.Log("[MontAR] Catálogo recriado: " + AssetDatabase.GetAssetPath(BuildCatalog(true)));
        }

        // ------------------------------------------------------------ etapas

        /// <summary>Importa o TMP Essential Resources se faltar. Falso quando acabou de importar.</summary>
        public static bool EnsureTmpEssentials()
        {
            if (File.Exists(TmpSettingsPath))
                return true;
            UnityEditor.AssetPackage.Package.Import(TmpEssentialsPackage, false);
            AssetDatabase.Refresh();
            return false;
        }

        public static string RebuildApp()
        {
            if (!File.Exists(TmpSettingsPath))
                throw new InvalidOperationException("Falta o TMP Essential Resources. Rode EnsureTmpEssentials antes.");

            ConfigureUiSprites();
            ConfigurePlayer();
            ConfigureGlass();
            UiFactory.Font = FontBuilder.EnsureFonts();
            PartCatalog catalog = BuildCatalog(false);
            var procedure = AssetDatabase.LoadAssetAtPath<AssemblyProcedure>(MontagemPreviewBuilder.ProcedurePath);
            if (procedure == null)
                throw new InvalidOperationException("Roteiro não encontrado. Rode MontAR › Prévia › Reconstruir cena de montagem antes.");

            string summary = $"App montado: {catalog.Parts.Count} peças, roteiro '{procedure.ProcedureId}' com {procedure.Steps.Count} etapas, interface {UiPrefabPath}, cena {ScenePath}.";
            BuildUiPrefab();
            AssetDatabase.SaveAssets();
            SetupScene();
            AssetDatabase.SaveAssets();
            return summary;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        }

        // Bordas do 9-slice (px) dos sprites que esticam; os demais são simples.
        private static readonly Dictionary<string, float> SliceBorders = new Dictionary<string, float>
        {
            { "ui_arredondado", 48f }, { "ui_contorno", 48f }, { "ui_brilho", 48f }, { "ui_gradiente", 48f }, { "ui_sombra", 88f },
        };

        private static void ConfigureUiSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesFolder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                string name = Path.GetFileNameWithoutExtension(path);
                var border = SliceBorders.TryGetValue(name, out float slice) ? new Vector4(slice, slice, slice, slice) : Vector4.zero;
                bool background = name.StartsWith("fundo_");
                int maxSize = background ? 2048 : importer.maxTextureSize;
                bool changed = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteBorder != border
                    || importer.mipmapEnabled
                    || importer.alphaIsTransparency == background
                    || importer.maxTextureSize != maxSize;
                if (!changed)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = !background;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = maxSize;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------ vidro

        public const string GlassShaderPath = "Assets/Shaders/UIGlass.shader";
        public const string GlassBlurShaderPath = "Assets/Shaders/GlassBlur.shader";
        public const string GlassMaterialPath = "Assets/Materials/UI/Vidro.mat";
        public const string GlassDenseMaterialPath = "Assets/Materials/UI/Vidro_denso.mat";
        private static readonly string[] RendererPaths = { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" };

        /// <summary>
        /// Materiais do vidro da interface e o desfoque da câmera (GlassBlurFeature) nos renderers do URP.
        /// O desfoque vai para o asset do renderer por código (o Inspector faz o mesmo ao clicar em Add Renderer Feature).
        /// </summary>
        private static void ConfigureGlass()
        {
            var glassShader = AssetDatabase.LoadAssetAtPath<Shader>(GlassShaderPath);
            var blurShader = AssetDatabase.LoadAssetAtPath<Shader>(GlassBlurShaderPath);
            if (glassShader == null || blurShader == null)
                throw new InvalidOperationException("Shaders do vidro não encontrados em Assets/Shaders.");

            EnsureFolder(Path.GetDirectoryName(GlassMaterialPath));
            UiFactory.GlassMaterial = GlassMaterial(GlassMaterialPath, glassShader, 0.66f);
            UiFactory.GlassDenseMaterial = GlassMaterial(GlassDenseMaterialPath, glassShader, 0.8f);

            foreach (string rendererPath in RendererPaths)
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (data == null)
                    continue;

                GlassBlurFeature feature = data.rendererFeatures.Find(f => f is GlassBlurFeature) as GlassBlurFeature;
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<GlassBlurFeature>();
                    feature.name = "MontAR Glass Blur";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

                    var so = new SerializedObject(data);
                    SerializedProperty features = so.FindProperty("m_RendererFeatures");
                    SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
                    features.arraySize++;
                    features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Bind(feature, ("blurShader", blurShader));
                feature.SetActive(true);
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
        }

        private static Material GlassMaterial(string path, Shader shader, float tintStrength)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetFloat("_TintStrength", tintStrength);
            material.SetFloat("_Saturation", 1.45f);
            material.SetFloat("_Brightness", 0.95f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------ catálogo

        private struct ModelSpec
        {
            public string Id, Name, Detail;

            public ModelSpec(string id, string name, string detail = "")
            {
                Id = id;
                Name = name;
                Detail = detail;
            }
        }

        private struct PartSpec
        {
            public string Id, Name, Description, Icon, UnitSingular, UnitPlural;
            public int Min, Max, Default;
            // O primeiro modelo é o do kit padrão. Os ids vão para o CSV e para o progresso salvo: não renomear.
            public ModelSpec[] Models;
        }

        private static readonly PartSpec[] DefaultParts =
        {
            new PartSpec { Id = "placa_mae", Name = "Placa-mãe", Icon = "peca_placa_mae", UnitSingular = "placa", UnitPlural = "placas", Min = 1, Max = 1, Default = 1,
                Description = "Base da montagem: as outras peças encaixam nela. Fica deitada no tapete.",
                Models = new[] { new ModelSpec("atx_generica_lga1700", "ATX genérica · LGA1700/AM5",
                    "Exemplo até definir o kit (D5): áudio HD 7.1 (codec Realtek ALC), rede 2,5 Gb/s, Wi-Fi 6E e Bluetooth 5.3, "
                    + "USB-C 3.2 e 6 USB-A atrás, 4 slots de RAM. O soquete acompanha a CPU.") } },
            new PartSpec { Id = "cpu", Name = "Processador (CPU)", Icon = "peca_cpu", UnitSingular = "unidade", UnitPlural = "unidades", Min = 1, Max = 1, Default = 1,
                Description = "Vai no soquete; o triângulo dourado mostra o sentido. Intel e AMD têm etapas próprias de CPU e cooler.",
                Models = new[]
                {
                    new ModelSpec("intel_lga1700", "Intel · soquete LGA1700"),
                    new ModelSpec("amd_am5", "AMD · soquete AM5"),
                } },
            new PartSpec { Id = "cooler", Name = "Cooler do processador", Icon = "peca_cooler", UnitSingular = "unidade", UnitPlural = "unidades", Min = 1, Max = 1, Default = 1,
                Description = "Tira o calor da CPU; o cabo vai no conector CPU_FAN.",
                Models = new[]
                {
                    new ModelSpec("cooler_ar_4pinos", "Torre a ar · 4 heat pipes · 120 mm",
                        "Heat pipes são os tubos de cobre que levam o calor da base até as aletas: mais tubos, mais calor dissipado. Cabo de 4 pinos (PWM)."),
                    new ModelSpec("cooler_box", "Cooler box (de fábrica) · sem heat pipes",
                        "Dissipador baixo de alumínio que vem com algumas CPUs: só para uso leve. Cabo de 4 pinos."),
                    new ModelSpec("cooler_torre_dupla", "Torre dupla · 6 heat pipes · 2 fans",
                        "Para CPUs que esquentam mais. É alto: confira a altura máxima do gabinete. Os 2 fans ligam juntos no CPU_FAN (cabo em Y)."),
                } },
            new PartSpec { Id = "ram", Name = "Memória RAM", Icon = "peca_ram", UnitSingular = "pente", UnitPlural = "pentes", Min = 1, Max = 4, Default = 2,
                Description = "Recomendado: 2 pentes iguais (dual channel). A placa aceita até 4; o roteiro indica os slots de cada caso.",
                Models = new[]
                {
                    new ModelSpec("ddr5_dimm", "DDR5 · 16 GB · 5600 MT/s · CL40",
                        "Capacidade por pente. Frequência (MT/s) e latência (CL) mudam o desempenho, não o encaixe: o roteiro é o mesmo."),
                    new ModelSpec("ddr5_8gb_4800_cl40", "DDR5 · 8 GB · 4800 MT/s · CL40",
                        "Capacidade por pente. Use pentes iguais (mesma capacidade, frequência e latência)."),
                    new ModelSpec("ddr5_32gb_6000_cl30", "DDR5 · 32 GB · 6000 MT/s · CL30",
                        "Capacidade por pente. Para chegar a 6000 MT/s, ative o perfil XMP/EXPO na BIOS depois de montar."),
                } },
            new PartSpec { Id = "ssd", Name = "Armazenamento", Icon = "peca_ssd", UnitSingular = "unidade", UnitPlural = "unidades", Min = 0, Max = 1, Default = 1,
                Description = "Onde ficam o sistema e os arquivos. Cada tipo liga de um jeito: o roteiro muda com o modelo.",
                Models = new[]
                {
                    new ModelSpec("ssd_m2_2280_nvme", "SSD M.2 2280 · NVMe",
                        "O mais rápido: encaixa direto na placa, inclinado, e prende com um parafuso. Sem cabos."),
                    new ModelSpec("ssd_sata_25", "SSD SATA 2,5\"",
                        "Liga por dois cabos: dados SATA (na placa) e energia SATA (da fonte). Mais lento que o NVMe."),
                    new ModelSpec("hd_sata_35", "HD SATA 3,5\" · 7200 rpm",
                        "Disco com peças móveis: mais espaço por preço, mais lento e frágil a batidas. Usa os mesmos cabos do SSD SATA."),
                } },
            new PartSpec { Id = "gpu", Name = "Placa de vídeo", Icon = "peca_gpu", UnitSingular = "unidade", UnitPlural = "unidades", Min = 0, Max = 1, Default = 1,
                Description = "Opcional: vai no slot PCIe x16 de cima. O tipo define o cabo de energia.",
                Models = new[]
                {
                    new ModelSpec("gpu_pcie_x16", "PCIe x16 · 1 cabo PCIe 8 pinos (6+2)",
                        "Intermediária: além do slot, recebe energia por um cabo PCIe 6+2 da fonte. Ocupa 2 slots de altura."),
                    new ModelSpec("gpu_sem_cabo", "PCIe x16 · sem cabo de energia (até 75 W)",
                        "De entrada: toda a energia vem do próprio slot PCIe. Não tem conector de energia na placa."),
                    new ModelSpec("gpu_12v2x6", "PCIe x16 · conector 12V-2x6 (16 pinos)",
                        "Alto desempenho: um cabo único de 16 pinos (12V-2x6/12VHPWR). Exige fonte com esse cabo ou o adaptador da placa. Ocupa 3 slots ou mais."),
                } },
            new PartSpec { Id = "fonte", Name = "Fonte de alimentação", Icon = "peca_fonte", UnitSingular = "unidade", UnitPlural = "unidades", Min = 1, Max = 1, Default = 1,
                Description = "Liga a placa pelos cabos de 24 pinos e EPS 8 pinos. Tem chave 110/220 V atrás? Escolha o modelo com chave.",
                Models = new[]
                {
                    new ModelSpec("fonte_atx", "ATX bivolt automática (100–240 V)"),
                    new ModelSpec("fonte_atx_chave", "ATX com chave 110/220 V",
                        "Fonte com chave seletora de tensão: o roteiro ganha uma etapa para conferir a chave antes de ligar."),
                } },
        };

        public static PartCatalog BuildCatalog(bool overwrite)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PartCatalog>(CatalogPath);
            if (catalog != null && !overwrite)
                return catalog;

            if (catalog == null)
            {
                EnsureFolder(Path.GetDirectoryName(CatalogPath));
                catalog = ScriptableObject.CreateInstance<PartCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            SerializedProperty parts = so.FindProperty("parts");
            parts.arraySize = DefaultParts.Length;
            for (int i = 0; i < DefaultParts.Length; i++)
            {
                PartSpec spec = DefaultParts[i];
                SerializedProperty part = parts.GetArrayElementAtIndex(i);
                part.FindPropertyRelative("partId").stringValue = spec.Id;
                part.FindPropertyRelative("displayName").stringValue = spec.Name;
                part.FindPropertyRelative("description").stringValue = spec.Description;
                part.FindPropertyRelative("icon").objectReferenceValue = LoadSprite(spec.Icon);
                part.FindPropertyRelative("unitSingular").stringValue = spec.UnitSingular;
                part.FindPropertyRelative("unitPlural").stringValue = spec.UnitPlural;
                part.FindPropertyRelative("minQuantity").intValue = spec.Min;
                part.FindPropertyRelative("maxQuantity").intValue = spec.Max;
                part.FindPropertyRelative("defaultQuantity").intValue = spec.Default;
                SerializedProperty models = part.FindPropertyRelative("models");
                models.arraySize = spec.Models.Length;
                for (int m = 0; m < spec.Models.Length; m++)
                {
                    SerializedProperty model = models.GetArrayElementAtIndex(m);
                    model.FindPropertyRelative("modelId").stringValue = spec.Models[m].Id;
                    model.FindPropertyRelative("displayName").stringValue = spec.Models[m].Name;
                    model.FindPropertyRelative("detail").stringValue = spec.Models[m].Detail ?? string.Empty;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        // ------------------------------------------------------------ interface

        private static void BuildUiPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UiPrefabPath));

            var root = new GameObject(UiRootName, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            root.AddComponent<GraphicRaycaster>();

            RectTransform screens = Stretch(Node("Telas", root.transform));
            WelcomeScreen welcome = BuildWelcome(screens);
            PartsScreen parts = BuildParts(screens);
            StartScreen start = BuildStart(screens);
            AssemblyScreen assembly = BuildAssembly(screens);
            CompletionScreen completion = BuildCompletion(screens);
            HistoryScreen history = BuildHistory(screens);
            BuildToast(root.transform);
            ConfirmDialog dialog = BuildDialog(root.transform);

            foreach (UiScreen screen in new UiScreen[] { welcome, parts, start, assembly, completion, history })
                screen.gameObject.SetActive(false);
            dialog.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>
        /// Tela de fundo inteiro, transparente: atrás dela aparece o fundo aurora (menus e bancada 3D) ou a
        /// câmera (RA). O conteúdo fica dentro da área segura.
        /// </summary>
        private static RectTransform ScreenRoot<T>(Transform parent, string name, out T screen) where T : UiScreen
        {
            RectTransform root = Stretch(Node(name, parent));
            root.gameObject.AddComponent<CanvasGroup>();
            screen = root.gameObject.AddComponent<T>();

            RectTransform safe = Stretch(Node("Área segura", root));
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        private static WelcomeScreen BuildWelcome(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 1 - Apresentação", out WelcomeScreen screen);
            VLayout(content.gameObject, Pad(56, 56, 36, 44), 0f, TextAnchor.UpperCenter);

            // Topo: "‹ Voltar" no canto esquerdo (longe do Próximo, que ocupa a base) e "Pular" no direito.
            RectTransform top = Node("Topo", content);
            HLayout(top.gameObject, null, 16f, TextAnchor.MiddleCenter);
            Layout(top.gameObject, preferredHeight: 110f);
            RectTransform backSlot = Node("Lugar do voltar", top);
            Layout(backSlot.gameObject, preferredHeight: 88f, preferredWidth: 220f, minWidth: 220f);
            Button back = MakeButton(backSlot, "Botão Voltar", "‹ Voltar", ButtonStyle.Ghost, out TextMeshProUGUI backLabel, 88f, 36f);
            Stretch((RectTransform)back.transform);
            backLabel.alignment = TextAlignmentOptions.MidlineLeft;
            backLabel.rectTransform.offsetMin = Vector2.zero;
            TextMeshProUGUI logo = Title(top, "Logo", "Mont<color=#4ADE80>AR</color>", 60f, TextAlignmentOptions.Center);
            Layout(logo.gameObject, flexibleWidth: 1f);
            Button skip = MakeButton(top, "Botão Pular", "Pular", ButtonStyle.Ghost, out TextMeshProUGUI skipLabel, 88f, 36f);
            skipLabel.alignment = TextAlignmentOptions.MidlineRight;
            skipLabel.rectTransform.offsetMax = Vector2.zero;
            Layout(skip.gameObject, preferredHeight: 88f, preferredWidth: 220f, minWidth: 220f);

            Spacer(content, 1f);
            RectTransform iconRow = Node("Linha do ícone", content);
            HLayout(iconRow.gameObject, null, 0f, TextAnchor.MiddleCenter);
            Image icon = GlassBadge(iconRow, "Ícone da página", LoadSprite("icone_cubo"), 340f, UiTheme.PrimaryGlow, UiTheme.TextAccent, 0.5f);
            Spacer(content, 0f, 72f);

            // Título e texto num cartão de vidro: leem bem sobre qualquer parte do fundo.
            RectTransform card = Node("Cartão", content);
            Glass(card.gameObject, 52f);
            VLayout(card.gameObject, Pad(56, 56, 52, 58), 22f, TextAnchor.UpperCenter);
            TextMeshProUGUI title = Title(card, "Título", "Bem-vindo ao MontAR", 66f, TextAlignmentOptions.Top);
            TextMeshProUGUI body = Label(card, "Texto", "", 38f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Top);
            body.lineSpacing = 10f;
            Spacer(content, 1.2f);

            RectTransform dots = Node("Pontos", content);
            HLayout(dots.gameObject, null, 14f, TextAnchor.MiddleCenter, false, false);
            Layout(dots.gameObject, preferredHeight: 30f);
            RectTransform dot = Node("Ponto", dots);
            dot.sizeDelta = new Vector2(18f, 18f);
            Image dotImage = Card(dot.gameObject, UiTheme.GlassBorderStrong, 9f);
            dotImage.raycastTarget = false;
            Spacer(content, 0f, 44f);

            Button next = MakeButton(content, "Botão Próximo", "Próximo", ButtonStyle.Primary, out TextMeshProUGUI nextLabel, 136f, 42f);

            Spacer(content, 0f, 28f);
            TextMeshProUGUI version = Label(content, "Versão (toque longo: diagnóstico)", "versão", 26f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            version.raycastTarget = true;
            var diagnostics = version.gameObject.AddComponent<LongPressButton>();
            Bind(diagnostics, ("holdSeconds", 2f));

            Bind(screen,
                ("pageIcon", icon), ("pageTitle", title), ("pageBody", body), ("dotsContainer", dots), ("dotTemplate", dotImage),
                ("nextButton", next), ("nextLabel", nextLabel), ("backButton", back), ("skipButton", skip), ("versionLabel", version));

            var so = new SerializedObject(screen);
            SerializedProperty pages = so.FindProperty("pages");
            var content5 = new[]
            {
                ("Bem-vindo ao MontAR", "Monte um computador com a instrução em 3D em cima da peça. O app guia uma etapa por vez e só libera a próxima depois que você confirma a anterior.", "icone_cubo"),
                ("1. Aponta", "Aponte a câmera do celular para o marcador A do tapete de montagem. É por ele que o app sabe onde fica cada encaixe da placa-mãe.", "icone_aponta"),
                ("2. Guia", "Uma peça fantasma em 3D mostra onde e em que sentido encaixar. Faça o mesmo movimento com a peça. Sem o kit? Toque em Simular encaixe e veja a peça entrar no lugar.", "icone_guia"),
                ("3. Validação", "Terminou a etapa? Toque em Validar e confira a lista. Se algo não bate, a etapa volta para você corrigir antes de seguir.", "icone_valida"),
                ("4. Registra", "Cada etapa validada fica salva. Se o app fechar, a montagem continua de onde parou.", "icone_registra"),
                ("Antes de começar", "Marcador A impresso sobre a mesa, à vista. Com o kit: peças separadas ao lado e a fonte fora da tomada. Sem o kit: escolha Peças virtuais na tela de início.", "icone_camera"),
            };
            pages.arraySize = content5.Length;
            for (int i = 0; i < content5.Length; i++)
            {
                SerializedProperty page = pages.GetArrayElementAtIndex(i);
                page.FindPropertyRelative("title").stringValue = content5[i].Item1;
                page.FindPropertyRelative("body").stringValue = content5[i].Item2;
                page.FindPropertyRelative("icon").objectReferenceValue = LoadSprite(content5[i].Item3);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        private static RectTransform Header(Transform content, string title, string subtitle, out Button back)
        {
            return Header(content, title, subtitle, out back, null, out _);
        }

        /// <summary>Cabeçalho com "‹ Voltar" e, se <paramref name="actionText"/> vier, um botão à direita na mesma linha.</summary>
        private static RectTransform Header(Transform content, string title, string subtitle, out Button back, string actionText, out Button action)
        {
            RectTransform header = Node("Cabeçalho", content);
            VLayout(header.gameObject, Pad(56, 56, 20, 28), 12f);
            RectTransform backRow = Node("Linha do voltar", header);
            HLayout(backRow.gameObject, null, 0f, TextAnchor.MiddleLeft);
            Layout(backRow.gameObject, preferredHeight: 88f);
            back = MakeButton(backRow, "Botão Voltar", "‹ Voltar", ButtonStyle.Ghost, out TextMeshProUGUI backLabel, 88f, 36f);
            backLabel.alignment = TextAlignmentOptions.MidlineLeft;
            backLabel.rectTransform.offsetMin = new Vector2(0f, 0f);
            Layout(back.gameObject, preferredHeight: 88f, preferredWidth: 220f);
            action = null;
            if (!string.IsNullOrEmpty(actionText))
            {
                Layout(Node("Espaço", backRow).gameObject, flexibleWidth: 1f);
                action = MakeButton(backRow, "Botão " + actionText, actionText, ButtonStyle.Ghost, out TextMeshProUGUI actionLabel, 88f, 34f);
                actionLabel.alignment = TextAlignmentOptions.MidlineRight;
                actionLabel.rectTransform.offsetMax = new Vector2(0f, 0f);
                Layout(action.gameObject, preferredHeight: 88f, preferredWidth: 360f);
            }
            Title(header, "Título", title, 64f);
            TextMeshProUGUI sub = Label(header, "Subtítulo", subtitle, 34f, UiTheme.TextSecondary);
            sub.lineSpacing = 8f;
            return header;
        }

        /// <summary>Rodapé: cartão de vidro flutuante com os botões (devolve o cartão).</summary>
        private static RectTransform Footer(Transform content)
        {
            RectTransform outer = Node("Rodapé", content);
            VLayout(outer.gameObject, Pad(24, 24, 12, 28), 0f);
            RectTransform card = Node("Cartão do rodapé", outer);
            Glass(card.gameObject, 52f, true);
            VLayout(card.gameObject, Pad(40, 40, 32, 36), 18f);
            return card;
        }

        /// <summary>Seletor de duas opções numa trilha escura; a tela pinta a opção escolhida.</summary>
        private static RectTransform Segmented(Transform parent, string name, string leftName, string leftText, string rightName, string rightText,
            out Button left, out TextMeshProUGUI leftLabel, out Button right, out TextMeshProUGUI rightLabel, float height = 124f, float fontSize = 34f)
        {
            RectTransform segmented = Node(name, parent);
            Fill(segmented.gameObject, new Color(0f, 0f, 0f, 0.28f), Mathf.Min(48f, height * 0.5f), UiTheme.GlassBorder);
            HLayout(segmented.gameObject, Pad(8), 8f, TextAnchor.MiddleCenter);
            Layout(segmented.gameObject, preferredHeight: height);
            float inner = height - 16f;
            left = MakeButton(segmented, leftName, leftText, ButtonStyle.Segment, out leftLabel, inner, fontSize);
            Layout(left.gameObject, preferredHeight: inner, flexibleWidth: 1f, preferredWidth: 0f);
            right = MakeButton(segmented, rightName, rightText, ButtonStyle.Segment, out rightLabel, inner, fontSize);
            Layout(right.gameObject, preferredHeight: inner, flexibleWidth: 1f, preferredWidth: 0f);
            return segmented;
        }

        private static PartsScreen BuildParts(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 2 - Peças", out PartsScreen screen);
            VLayout(content.gameObject, null, 0f);
            Header(content, "Escolha as peças", "Use ‹ › para trocar o modelo (ex.: Intel ou AMD). O roteiro se ajusta ao que você marcar.", out Button back,
                "Histórico ›", out Button history);

            RectTransform list = Scroll(content, "Lista de peças", Pad(32, 32, 8, 40), 20f, out ScrollRect scroll);
            PartRowView row = BuildPartRow(list);

            RectTransform footer = Footer(content);
            TextMeshProUGUI summary = Label(footer, "Resumo", "O roteiro terá 9 etapas", 32f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center);
            Button next = MakeButton(footer, "Botão Continuar", "Continuar", ButtonStyle.Primary, out _, 132f);

            Bind(screen, ("listContainer", list), ("rowTemplate", row), ("summaryLabel", summary), ("continueButton", next),
                ("backButton", back), ("historyButton", history), ("scrollRect", scroll));
            return screen;
        }

        private static PartRowView BuildPartRow(Transform parent)
        {
            RectTransform row = Node("Peça (modelo)", parent);
            Glass(row.gameObject, 40f);
            HLayout(row.gameObject, Pad(32, 28, 32, 32), 20f, TextAnchor.MiddleLeft);

            RectTransform dimmed = Node("Peça", row);
            HLayout(dimmed.gameObject, null, 28f, TextAnchor.MiddleLeft);
            Layout(dimmed.gameObject, flexibleWidth: 1f);
            CanvasGroup group = dimmed.gameObject.AddComponent<CanvasGroup>();
            Image icon = Badge(dimmed, "Ícone", null, UiTheme.PrimarySoft, UiTheme.TextAccent, 120f, 0.62f);

            RectTransform texts = Node("Textos", dimmed);
            VLayout(texts.gameObject, null, 6f);
            Layout(texts.gameObject, flexibleWidth: 1f);
            TextMeshProUGUI name = Label(texts, "Nome", "Peça", 40f, UiTheme.TextPrimary, FontStyles.Bold);
            RectTransform modelRow = Node("Modelo", texts);
            HLayout(modelRow.gameObject, null, 8f, TextAnchor.MiddleLeft);
            Button previous = IconButton(modelRow, "Modelo anterior", "‹", null, UiTheme.GlassFill, UiTheme.TextPrimary, 56f, 40f, out _);
            TextMeshProUGUI model = Label(modelRow, "Nome do modelo", "Modelo", 30f, UiTheme.TextAccent, FontStyles.Bold);
            Layout(model.gameObject, flexibleWidth: 1f);
            Button nextModel = IconButton(modelRow, "Próximo modelo", "›", null, UiTheme.GlassFill, UiTheme.TextPrimary, 56f, 40f, out _);
            TextMeshProUGUI modelDetail = Label(texts, "Detalhe do modelo", "", 26f, UiTheme.TextSecondary);
            modelDetail.lineSpacing = 2f;
            TextMeshProUGUI description = Label(texts, "Descrição", "", 28f, UiTheme.TextMuted);
            TextMeshProUGUI requirement = Label(texts, "Obrigatória ou opcional", "obrigatória", 22f, UiTheme.TextMuted, FontStyles.Bold | FontStyles.UpperCase);
            requirement.characterSpacing = 4f;

            RectTransform stepper = Node("Quantidade", row);
            VLayout(stepper.gameObject, null, 10f, TextAnchor.MiddleCenter, true, false);
            Layout(stepper.gameObject, preferredWidth: 200f, minWidth: 200f, flexibleWidth: 0f);
            RectTransform buttons = Node("Botões", stepper);
            HLayout(buttons.gameObject, null, 18f, TextAnchor.MiddleCenter);
            Button minus = IconButton(buttons, "Menos", "–", null, UiTheme.GlassFillStrong, UiTheme.TextPrimary, 84f, 48f, out _);
            Button plus = IconButton(buttons, "Mais", "+", null, UiTheme.GlassFillStrong, UiTheme.TextPrimary, 84f, 48f, out _);
            CanvasGroup buttonsGroup = buttons.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI quantity = Label(stepper, "Quantidade escolhida", "1 unidade", 28f, UiTheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);

            var view = row.gameObject.AddComponent<PartRowView>();
            Bind(view, ("icon", icon), ("nameLabel", name), ("modelLabel", model), ("modelDetailLabel", modelDetail), ("descriptionLabel", description),
                ("requirementLabel", requirement), ("previousModelButton", previous), ("nextModelButton", nextModel),
                ("minusButton", minus), ("plusButton", plus), ("quantityLabel", quantity), ("quantityButtons", buttonsGroup), ("dimmedGroup", group));
            return view;
        }

        private static RectTransform CardSection(Transform parent, string name, string title, out TextMeshProUGUI titleLabel)
        {
            RectTransform card = Node(name, parent);
            Glass(card.gameObject, 40f);
            VLayout(card.gameObject, Pad(40, 40, 36, 40), 18f);
            titleLabel = Label(card, "Título", title, 38f, UiTheme.TextPrimary, FontStyles.Bold);
            return card;
        }

        private static StartScreen BuildStart(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 3 - Início", out StartScreen screen);
            VLayout(content.gameObject, null, 0f);
            Header(content, "Tudo pronto?", "Informe o código do participante e escolha como ver a instrução.", out Button back);

            RectTransform scrollContent = Scroll(content, "Opções", Pad(32, 32, 8, 40), 22f, out _);

            RectTransform participant = CardSection(scrollContent, "Participante", "Código do participante", out _);
            TMP_InputField input = TextInput(participant, "Campo do código", "P01", out _);
            Label(participant, "Dica", "Código anônimo, como P01. Nunca use nome ou RA.", 28f, UiTheme.TextMuted);
            TextMeshProUGUI error = Label(participant, "Erro", "", 28f, UiTheme.DangerText, FontStyles.Bold);
            error.gameObject.SetActive(false);

            RectTransform labelCard = CardSection(scrollContent, "Nome da montagem", "Nome da montagem (opcional)", out _);
            TMP_InputField labelInput = TextInput(labelCard, "Campo do nome", "Ex.: Bancada 3 · HP", out _);
            Label(labelCard, "Dica", "Ajuda a achar a montagem no histórico. Não use o nome do participante.", 28f, UiTheme.TextMuted);

            RectTransform partsCard = CardSection(scrollContent, "Peças", "Peças da montagem", out _);
            Segmented(partsCard, "Escolha das peças", "Peças reais", "Kit de verdade", "Peças virtuais", "Peças virtuais",
                out Button partsReal, out TextMeshProUGUI partsRealLabel, out Button partsVirtual, out TextMeshProUGUI partsVirtualLabel);
            TextMeshProUGUI partsNote = Label(partsCard, "Explicação das peças", "", 30f, UiTheme.TextSecondary);
            partsNote.lineSpacing = 6f;

            RectTransform modeCard = CardSection(scrollContent, "Modo", "Como ver a instrução", out _);
            Segmented(modeCard, "Escolha do modo", "Modo RA", "Câmera (RA)", "Modo 3D", "Demonstração 3D",
                out Button ar, out TextMeshProUGUI arLabel, out Button demo, out TextMeshProUGUI demoLabel);
            TextMeshProUGUI note = Label(modeCard, "Explicação do modo", "", 30f, UiTheme.TextSecondary);
            note.lineSpacing = 6f;

            RectTransform stepsCard = CardSection(scrollContent, "Roteiro", "Roteiro", out TextMeshProUGUI stepsHeader);
            TextMeshProUGUI steps = Label(stepsCard, "Etapas", "", 32f, UiTheme.TextSecondary);
            steps.lineSpacing = 22f;

            RectTransform footer = Footer(content);
            Button startButton = MakeButton(footer, "Botão Iniciar", "Iniciar montagem", ButtonStyle.Primary, out _, 132f);

            Bind(screen, ("participantInput", input), ("participantError", error), ("labelInput", labelInput),
                ("modeArButton", ar), ("modeArBackground", ar.targetGraphic), ("modeArLabel", arLabel),
                ("modeDemoButton", demo), ("modeDemoBackground", demo.targetGraphic), ("modeDemoLabel", demoLabel),
                ("partsRealButton", partsReal), ("partsRealBackground", partsReal.targetGraphic), ("partsRealLabel", partsRealLabel),
                ("partsVirtualButton", partsVirtual), ("partsVirtualBackground", partsVirtual.targetGraphic), ("partsVirtualLabel", partsVirtualLabel),
                ("partsNote", partsNote),
                ("modeNote", note), ("stepsHeader", stepsHeader), ("stepsList", steps), ("startButton", startButton), ("backButton", back));
            return screen;
        }

        private static AssemblyScreen BuildAssembly(Transform parent)
        {
            RectTransform root = Stretch(Node("Tela 4 - Montagem", parent));
            root.gameObject.AddComponent<CanvasGroup>();
            var screen = root.gameObject.AddComponent<AssemblyScreen>();

            RectTransform orbit = Stretch(Node("Superfície de giro (3D)", root));
            Img(orbit.gameObject, new Color(0f, 0f, 0f, 0f));
            orbit.gameObject.AddComponent<DemoOrbitInput>();

            RectTransform safe = Stretch(Node("Área segura", root));
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // Topo: cartão da etapa (com a situação dela) e, embaixo, a linha do modo e do 3D.
            // A peça 3D fica centralizada entre o fim desta área e o painel da etapa.
            RectTransform topArea = TopBand(Node("Topo da montagem", safe), 24f, 420f, 24f, 24f);
            VLayout(topArea.gameObject, null, 16f);
            FitHeight(topArea.gameObject);

            // Cartão de vidro: etapa, componente, progresso, sair, botão discreto do observador e a situação da etapa.
            RectTransform top = Node("Topo", topArea);
            Glass(top.gameObject, 44f, true);
            VLayout(top.gameObject, Pad(30, 30, 26, 26), 18f);
            RectTransform row = Node("Linha", top);
            HLayout(row.gameObject, null, 24f, TextAnchor.MiddleLeft);
            Layout(row.gameObject, preferredHeight: 118f);
            Button exit = IconButton(row, "Botão Sair", "×", null, new Color(1f, 1f, 1f, 0.1f), UiTheme.TextOnDark, 92f, 56f, out _);
            RectTransform texts = Node("Textos", row);
            VLayout(texts.gameObject, null, 2f, TextAnchor.MiddleLeft);
            Layout(texts.gameObject, flexibleWidth: 1f);
            TextMeshProUGUI counter = Title(texts, "Etapa", "Etapa 1 de 9", 46f);
            TextMeshProUGUI component = Label(texts, "Componente", "Componente", 30f, UiTheme.TextOnDarkMuted);
            component.textWrappingMode = TextWrappingModes.NoWrap;
            component.overflowMode = TextOverflowModes.Ellipsis;

            // Observador: olho dentro do círculo e legenda. Toque curto explica, toque longo registra a ajuda.
            RectTransform observerGroup = Node("Observador", row);
            VLayout(observerGroup.gameObject, null, 4f, TextAnchor.MiddleCenter, true, false);
            Layout(observerGroup.gameObject, preferredWidth: 120f, minWidth: 120f);
            RectTransform observer = Node("Observador (toque longo)", observerGroup);
            Layout(observer.gameObject, preferredHeight: 64f, preferredWidth: 64f, minHeight: 64f, minWidth: 64f);
            Img(observer.gameObject, new Color(1f, 1f, 1f, 0.1f), Circle);
            Decorate(observer, 32f, UiTheme.GlassBorder, null, true);
            RectTransform fill = Stretch(Node("Progresso", observer));
            Image fillImage = Img(fill.gameObject, UiTheme.Primary, Circle);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
            RectTransform eye = Stretch(Node("Ícone", observer), 14f, 14f, 14f, 14f);
            Image eyeImage = Img(eye.gameObject, new Color(1f, 1f, 1f, 0.8f), LoadSprite("icone_observador"));
            eyeImage.preserveAspect = true;
            eyeImage.raycastTarget = false;
            TextMeshProUGUI observerCaption = Label(observerGroup, "Legenda", "observador", 20f, UiTheme.TextOnDarkMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            observerCaption.textWrappingMode = TextWrappingModes.NoWrap;
            var longPress = observer.gameObject.AddComponent<LongPressButton>();
            Bind(longPress, ("holdSeconds", 1.2f), ("progressFill", fillImage));

            RectTransform bar = Node("Barra de progresso", top);
            Layout(bar.gameObject, preferredHeight: 14f);
            Card(bar.gameObject, new Color(1f, 1f, 1f, 0.12f), 7f);
            RectTransform barFill = Stretch(Node("Preenchimento", bar));
            Image progress = Img(barFill.gameObject, UiTheme.Primary, Rounded, CornerScale(7f));
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillAmount = 0.1f;

            // Situação da etapa dentro do cartão do topo, com ícone. A cor vem da tela (âmbar, verde, azul).
            RectTransform chip = Node("Situação da etapa", top);
            Image chipImage = Card(chip.gameObject, UiTheme.StatusLocating, 30f);
            chipImage.raycastTarget = false;
            HLayout(chip.gameObject, Pad(22, 24, 14, 14), 18f, TextAnchor.MiddleLeft);
            Layout(chip.gameObject, minHeight: 84f);
            RectTransform statusIconRect = Node("Ícone", chip);
            Layout(statusIconRect.gameObject, preferredHeight: 52f, preferredWidth: 52f, minHeight: 52f, minWidth: 52f);
            Image statusIcon = Img(statusIconRect.gameObject, UiTheme.TextOnDark, LoadSprite("icone_cubo"));
            statusIcon.preserveAspect = true;
            statusIcon.raycastTarget = false;
            TextMeshProUGUI statusLabel = Label(chip, "Texto", "Aponte a câmera para o marcador A", 30f, UiTheme.TextOnDark, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Layout(statusLabel.gameObject, flexibleWidth: 1f);

            // Linha do modo: aviso do 3D (com cadeado) à esquerda e a troca Câmera/3D à direita, os dois em vidro.
            RectTransform status = Node("Modo e 3D", topArea);
            HLayout(status.gameObject, null, 14f, TextAnchor.MiddleRight);
            Layout(status.gameObject, preferredHeight: 84f, minHeight: 84f);
            RectTransform hint = Node("Aviso do 3D (cadeado)", status);
            Image hintBackground = Glass(hint.gameObject, 42f);
            hintBackground.raycastTarget = false;
            HLayout(hint.gameObject, Pad(22, 22, 8, 8), 14f, TextAnchor.MiddleLeft);
            Layout(hint.gameObject, preferredHeight: 84f, flexibleWidth: 1f, minWidth: 0f, preferredWidth: 0f);
            RectTransform lockRect = Node("Cadeado", hint);
            Layout(lockRect.gameObject, preferredHeight: 44f, preferredWidth: 44f, minHeight: 44f, minWidth: 44f);
            Image lockIcon = Img(lockRect.gameObject, UiTheme.TextOnDark, LoadSprite("icone_cadeado_aberto"));
            lockIcon.preserveAspect = true;
            lockIcon.raycastTarget = false;
            TextMeshProUGUI lockLabel = Label(hint, "Texto", "3D livre", 24f, UiTheme.TextOnDark, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Layout(lockLabel.gameObject, flexibleWidth: 1f);
            lockLabel.enableAutoSizing = true;
            lockLabel.fontSizeMin = 18f;
            lockLabel.fontSizeMax = 24f;

            RectTransform modes = Node("Modo", status);
            Glass(modes.gameObject, 42f);
            HLayout(modes.gameObject, Pad(6), 6f, TextAnchor.MiddleCenter);
            Layout(modes.gameObject, preferredHeight: 84f, preferredWidth: 360f, flexibleWidth: 0f, minWidth: 360f);
            Button ar = MakeButton(modes, "Modo RA", "Câmera", ButtonStyle.Segment, out TextMeshProUGUI arLabel, 72f, 28f);
            Layout(ar.gameObject, preferredHeight: 72f, flexibleWidth: 1f);
            Button demo = MakeButton(modes, "Modo 3D", "3D", ButtonStyle.Segment, out TextMeshProUGUI demoLabel, 72f, 28f);
            Layout(demo.gameObject, preferredHeight: 72f, flexibleWidth: 1f);

            // Painel da etapa (Guia), em vidro denso. Arrastar para cima/baixo mostra ou esconde a instrução.
            RectTransform sheet = BottomBand(Node("Painel da etapa", safe), 24f, 24f, 24f);
            Glass(sheet.gameObject, 52f, true);
            VLayout(sheet.gameObject, Pad(44, 44, 10, 40), 18f);
            FitHeight(sheet.gameObject);
            var sheetDrag = sheet.gameObject.AddComponent<SheetDragHandler>();

            RectTransform handle = Node("Esconder instrução", sheet);
            Image handleImage = Img(handle.gameObject, new Color(1f, 1f, 1f, 0f));
            var collapse = handle.gameObject.AddComponent<Button>();
            collapse.targetGraphic = handleImage;
            var collapseNav = collapse.navigation;
            collapseNav.mode = Navigation.Mode.None;
            collapse.navigation = collapseNav;
            VLayout(handle.gameObject, Pad(0, 0, 12, 4), 6f, TextAnchor.UpperCenter, true, false);
            Layout(handle.gameObject, preferredHeight: 74f);
            RectTransform grabber = Node("Puxador", handle);
            grabber.sizeDelta = new Vector2(110f, 10f);
            Card(grabber.gameObject, new Color(1f, 1f, 1f, 0.32f), 5f).raycastTarget = false;
            Layout(grabber.gameObject, preferredHeight: 10f, preferredWidth: 110f);
            TextMeshProUGUI collapseLabel = Label(handle, "Texto", "Arraste para baixo para esconder a instrução", 24f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            Layout(collapseLabel.gameObject, preferredHeight: 34f);

            TextMeshProUGUI title = Title(sheet, "Título", "Título da etapa", 46f);
            RectTransform details = Node("Detalhes", sheet);
            VLayout(details.gameObject, null, 20f);
            TextMeshProUGUI instruction = Label(details, "Instrução", "", 34f, UiTheme.TextSecondary);
            instruction.lineSpacing = 8f;
            RectTransform alert = Node("Alerta", details);
            Fill(alert.gameObject, UiTheme.AlertBackground, 28f, UiTheme.AlertBorder);
            VLayout(alert.gameObject, Pad(30, 30, 24, 26), 0f);
            TextMeshProUGUI alertLabel = Label(alert, "Texto", "", 31f, UiTheme.AlertText);
            alertLabel.lineSpacing = 6f;

            // Erro da validação: vermelho e logo acima do botão. Fica fora dos detalhes (não some ao esconder).
            RectTransform errorBox = Node("Erro da validação", sheet);
            Fill(errorBox.gameObject, UiTheme.Danger, 28f, UiTheme.DangerBorder);
            HLayout(errorBox.gameObject, Pad(28, 28, 24, 26), 20f, TextAnchor.UpperLeft);
            RectTransform errorIconRect = Node("Ícone", errorBox);
            Layout(errorIconRect.gameObject, preferredHeight: 52f, preferredWidth: 52f, minHeight: 52f, minWidth: 52f);
            Image errorIcon = Img(errorIconRect.gameObject, UiTheme.DangerText, LoadSprite("icone_alerta"));
            errorIcon.preserveAspect = true;
            errorIcon.raycastTarget = false;
            TextMeshProUGUI errorLabel = Label(errorBox, "Texto", "", 34f, UiTheme.DangerText);
            errorLabel.lineSpacing = 6f;
            Layout(errorLabel.gameObject, flexibleWidth: 1f);
            errorBox.gameObject.SetActive(false);

            // Botões: "Simular encaixe" (só com peças virtuais) ao lado do "Validar etapa".
            RectTransform actionsRow = Node("Botões da etapa", sheet);
            HLayout(actionsRow.gameObject, null, 20f, TextAnchor.MiddleCenter);
            Layout(actionsRow.gameObject, preferredHeight: 128f);
            Button simulate = IconTextButton(actionsRow, "Botão Simular encaixe", "Simular encaixe", LoadSprite("icone_encaixe"), ButtonStyle.Secondary,
                out TextMeshProUGUI simulateLabel, out Image simulateIcon, 128f, 34f);
            Layout(simulate.gameObject, preferredHeight: 128f, minHeight: 128f, flexibleWidth: 1f, preferredWidth: 0f);
            Button validate = MakeButton(actionsRow, "Botão Validar", "Validar etapa", ButtonStyle.Primary, out TextMeshProUGUI validateLabel, 128f, 38f);
            Layout(validate.gameObject, preferredHeight: 128f, minHeight: 128f, flexibleWidth: 1f, preferredWidth: 0f);

            // Painel da validação (Valida).
            RectTransform validation = BottomBand(Node("Painel da validação", safe), 24f, 24f, 24f);
            Glass(validation.gameObject, 52f, true);
            VLayout(validation.gameObject, Pad(44, 44, 40, 40), 22f);
            FitHeight(validation.gameObject);
            Title(validation, "Título", "Confira antes de avançar", 46f);
            TextMeshProUGUI validationHint = Label(validation, "Dica", "", 30f, UiTheme.TextSecondary);
            validationHint.lineSpacing = 6f;
            RectTransform checklist = Node("Checklist", validation);
            VLayout(checklist.gameObject, null, 14f);
            Toggle itemToggle = Checkbox(checklist, "Item", out TextMeshProUGUI itemLabel, out GameObject itemMark);
            var item = itemToggle.gameObject.AddComponent<ChecklistItemView>();
            Bind(item, ("toggle", itemToggle), ("label", itemLabel), ("checkMark", itemMark));
            RectTransform actions = Node("Botões", validation);
            HLayout(actions.gameObject, null, 20f, TextAnchor.MiddleCenter);
            Layout(actions.gameObject, preferredHeight: 128f);
            Button cancel = MakeButton(actions, "Botão Voltar", "Voltar", ButtonStyle.Secondary, out _, 128f);
            Layout(cancel.gameObject, preferredHeight: 128f, preferredWidth: 280f);
            Button confirm = MakeButton(actions, "Botão Confirmar", "Confirmar", ButtonStyle.Primary, out TextMeshProUGUI confirmLabel, 128f);
            Layout(confirm.gameObject, preferredHeight: 128f, flexibleWidth: 1f);
            validation.gameObject.SetActive(false);

            // Aviso curto ("Etapa concluída!") logo acima do painel; a tela reposiciona a cada quadro.
            Toast notice = BuildToastBand(safe, "Aviso da etapa", false);

            Bind(screen,
                ("exitButton", exit), ("stepCounter", counter), ("componentLabel", component), ("progressFill", progress), ("observerButton", longPress),
                ("statusRow", chip.gameObject), ("statusChip", chipImage), ("statusIcon", statusIcon), ("statusLabel", statusLabel),
                ("locatingSprite", LoadSprite("icone_aponta")), ("guidingSprite", LoadSprite("icone_guia")), ("demoSprite", LoadSprite("icone_cubo")),
                ("modeArButton", ar), ("modeArBackground", ar.targetGraphic), ("modeArLabel", arLabel),
                ("modeDemoButton", demo), ("modeDemoBackground", demo.targetGraphic), ("modeDemoLabel", demoLabel),
                ("viewHint", hint.gameObject), ("viewHintBackground", hintBackground), ("lockIcon", lockIcon), ("lockLabel", lockLabel),
                ("lockedSprite", LoadSprite("icone_cadeado_fechado")), ("unlockedSprite", LoadSprite("icone_cadeado_aberto")), ("topArea", topArea),
                ("stepSheet", sheet.gameObject), ("sheetDrag", sheetDrag), ("collapseButton", collapse), ("collapseLabel", collapseLabel), ("titleLabel", title),
                ("detailsGroup", details.gameObject), ("instructionLabel", instruction), ("alertBox", alert.gameObject), ("alertLabel", alertLabel),
                ("errorBox", errorBox.gameObject), ("errorLabel", errorLabel),
                ("validateButton", validate), ("validateLabel", validateLabel),
                ("simulateButton", simulate), ("simulateLabel", simulateLabel), ("simulateIcon", simulateIcon),
                ("fitSprite", LoadSprite("icone_encaixe")), ("fittedSprite", LoadSprite("ui_check")),
                ("validationSheet", validation.gameObject), ("validationHint", validationHint), ("checklistContainer", checklist),
                ("checklistTemplate", item), ("confirmButton", confirm), ("confirmLabel", confirmLabel), ("cancelButton", cancel),
                ("orbitSurface", orbit.gameObject), ("toast", notice));
            return screen;
        }

        private static CompletionScreen BuildCompletion(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 5 - Conclusão", out CompletionScreen screen);
            VLayout(content.gameObject, null, 0f);

            RectTransform scroll = Scroll(content, "Resumo", Pad(40, 40, 64, 32), 24f, out _);
            RectTransform badgeRow = Node("Linha do ícone", scroll);
            HLayout(badgeRow.gameObject, null, 0f, TextAnchor.MiddleCenter);
            Layout(badgeRow.gameObject, preferredHeight: 230f);
            GlassBadge(badgeRow, "Concluído", LoadSprite("ui_check"), 200f, UiTheme.PrimaryGlow, UiTheme.TextAccent, 0.5f);
            Title(scroll, "Título", "Montagem concluída!", 62f, TextAlignmentOptions.Top);
            TextMeshProUGUI subtitle = Label(scroll, "Subtítulo", "", 34f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Top);

            RectTransform grid = Node("Números", scroll);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(488f, 196f);
            gridLayout.spacing = new Vector2(24f, 24f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            Layout(grid.gameObject, preferredHeight: 416f);
            TextMeshProUGUI time = Stat(grid, "Tempo total");
            TextMeshProUGUI steps = Stat(grid, "Etapas validadas");
            TextMeshProUGUI failures = Stat(grid, "Validações reprovadas");
            TextMeshProUGUI interventions = Stat(grid, "Intervenções do observador");

            RectTransform timesCard = CardSection(scroll, "Tempo por etapa", "Tempo por etapa", out _);
            TextMeshProUGUI times = Label(timesCard, "Lista", "", 30f, UiTheme.TextSecondary);
            times.lineSpacing = 18f;
            TextMeshProUGUI file = Label(scroll, "Arquivo", "", 26f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Top);

            RectTransform footer = Footer(content);
            Button history = MakeButton(footer, "Botão Histórico", "Ver histórico de montagens", ButtonStyle.Primary, out _, 128f);
            Button again = MakeButton(footer, "Botão Nova montagem", "Nova montagem", ButtonStyle.Secondary, out _, 112f);

            Bind(screen, ("subtitleLabel", subtitle), ("timeValue", time), ("stepsValue", steps), ("failuresValue", failures),
                ("interventionsValue", interventions), ("stepTimesList", times), ("fileLabel", file),
                ("newSessionButton", again), ("historyButton", history));
            return screen;
        }

        private static HistoryScreen BuildHistory(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 6 - Histórico", out HistoryScreen screen);
            VLayout(content.gameObject, null, 0f);
            Header(content, "Histórico de montagens", "Cada montagem concluída neste aparelho: nome, tempo, erros apontados e etapas.", out Button back);

            RectTransform list = Scroll(content, "Lista de montagens", Pad(32, 32, 8, 40), 20f, out ScrollRect scroll);
            TextMeshProUGUI empty = Label(list, "Vazio", "Nenhuma montagem concluída ainda. Quando você terminar uma, ela aparece aqui.", 32f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            HistoryEntryView entry = BuildHistoryEntry(list);

            RectTransform footer = Footer(content);
            TextMeshProUGUI summary = Label(footer, "Resumo", "", 30f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center);
            Button again = MakeButton(footer, "Botão Nova montagem", "Nova montagem", ButtonStyle.Primary, out _, 128f);

            Bind(screen, ("listContainer", list), ("entryTemplate", entry), ("emptyLabel", empty), ("summaryLabel", summary),
                ("backButton", back), ("newSessionButton", again), ("scrollRect", scroll));
            return screen;
        }

        private static HistoryEntryView BuildHistoryEntry(Transform parent)
        {
            RectTransform card = Node("Montagem (modelo)", parent);
            Glass(card.gameObject, 40f);
            VLayout(card.gameObject, Pad(40, 40, 32, 28), 14f);

            RectTransform badgeRow = Node("Linha do selo", card);
            HLayout(badgeRow.gameObject, null, 0f, TextAnchor.MiddleLeft);
            RectTransform badge = Node("Recém-concluída", badgeRow);
            Fill(badge.gameObject, UiTheme.PrimarySoft, 22f, new Color(UiTheme.Primary.r, UiTheme.Primary.g, UiTheme.Primary.b, 0.4f)).raycastTarget = false;
            HLayout(badge.gameObject, Pad(18, 18, 6, 6), 0f, TextAnchor.MiddleCenter);
            TextMeshProUGUI badgeLabel = Label(badge, "Texto", "RECÉM-CONCLUÍDA", 22f, UiTheme.TextAccent, FontStyles.Bold, TextAlignmentOptions.Center);
            badgeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            badgeLabel.characterSpacing = 4f;

            RectTransform head = Node("Cabeçalho", card);
            HLayout(head.gameObject, null, 24f, TextAnchor.MiddleLeft);
            Badge(head, "Ícone", LoadSprite("icone_historico"), UiTheme.PrimarySoft, UiTheme.TextAccent, 96f, 0.6f);
            RectTransform texts = Node("Textos", head);
            VLayout(texts.gameObject, null, 4f);
            Layout(texts.gameObject, flexibleWidth: 1f);
            TextMeshProUGUI name = Label(texts, "Nome", "Montagem", 40f, UiTheme.TextPrimary, FontStyles.Bold);
            TextMeshProUGUI meta = Label(texts, "Data e participante", "", 26f, UiTheme.TextMuted);

            TextMeshProUGUI stats = Label(card, "Resumo", "", 30f, UiTheme.TextSecondary);
            TextMeshProUGUI steps = Label(card, "Etapas", "", 28f, UiTheme.TextSecondary);
            steps.lineSpacing = 14f;
            Button toggle = MakeButton(card, "Botão Ver etapas", "Ver etapas", ButtonStyle.Ghost, out TextMeshProUGUI toggleLabel, 72f, 30f);
            toggleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            toggleLabel.rectTransform.offsetMin = new Vector2(0f, 0f);

            var view = card.gameObject.AddComponent<HistoryEntryView>();
            Bind(view, ("nameLabel", name), ("metaLabel", meta), ("statsLabel", stats), ("stepsLabel", steps),
                ("toggleButton", toggle), ("toggleLabel", toggleLabel), ("latestBadge", badgeRow.gameObject));
            return view;
        }

        private static TextMeshProUGUI Stat(Transform grid, string label)
        {
            RectTransform card = Node(label, grid);
            Glass(card.gameObject, 40f);
            VLayout(card.gameObject, Pad(24, 24, 30, 24), 4f, TextAnchor.MiddleCenter);
            TextMeshProUGUI value = Title(card, "Valor", "0", 64f, TextAlignmentOptions.Center);
            Label(card, "Rótulo", label, 27f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            return value;
        }

        private static Toast BuildToast(Transform parent) => BuildToastBand(parent, "Aviso", true);

        /// <summary>
        /// Aviso curto em pílula de vidro. <paramref name="atTop"/>: faixa fixa no alto (menus). Senão, presa à
        /// base e reposicionada pela tela da montagem logo acima do painel da etapa.
        /// </summary>
        private static Toast BuildToastBand(Transform parent, string name, bool atTop)
        {
            RectTransform band = Node(name, parent);
            if (atTop)
            {
                TopBand(band, 470f, 120f, 80f, 80f);
            }
            else
            {
                band.anchorMin = new Vector2(0f, 0f);
                band.anchorMax = new Vector2(1f, 0f);
                band.pivot = new Vector2(0.5f, 0f);
                band.offsetMin = new Vector2(64f, 600f);
                band.offsetMax = new Vector2(-64f, 720f);
            }
            band.gameObject.AddComponent<CanvasGroup>();
            Glass(band.gameObject, 48f, true).raycastTarget = false;
            VLayout(band.gameObject, Pad(40, 40, 24, 24), 0f, TextAnchor.MiddleCenter);
            FitHeight(band.gameObject);
            TextMeshProUGUI label = Label(band, "Texto", "Aviso", 32f, UiTheme.TextOnDark, FontStyles.Bold, TextAlignmentOptions.Center);
            var toast = band.gameObject.AddComponent<Toast>();
            Bind(toast, ("label", label));
            return toast;
        }

        private static ConfirmDialog BuildDialog(Transform parent)
        {
            RectTransform root = Stretch(Node("Diálogo", parent));
            Img(root.gameObject, UiTheme.Scrim);
            var dialog = root.gameObject.AddComponent<ConfirmDialog>();

            RectTransform card = Node("Cartão", root);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(960f, 400f);
            Glass(card.gameObject, 52f, true);
            VLayout(card.gameObject, Pad(52, 52, 52, 44), 26f);
            FitHeight(card.gameObject);
            TextMeshProUGUI title = Title(card, "Título", "Título", 48f);
            TextMeshProUGUI message = Label(card, "Mensagem", "", 34f, UiTheme.TextSecondary);
            message.lineSpacing = 8f;
            RectTransform buttons = Node("Botões", card);
            HLayout(buttons.gameObject, Pad(0, 0, 12, 0), 20f, TextAnchor.MiddleCenter);
            Layout(buttons.gameObject, preferredHeight: 136f);
            Button secondary = MakeButton(buttons, "Botão secundário", "Cancelar", ButtonStyle.Secondary, out TextMeshProUGUI secondaryLabel, 124f, 36f);
            Layout(secondary.gameObject, preferredHeight: 124f, flexibleWidth: 1f);
            Button primary = MakeButton(buttons, "Botão principal", "OK", ButtonStyle.Primary, out TextMeshProUGUI primaryLabel, 124f, 36f);
            Layout(primary.gameObject, preferredHeight: 124f, flexibleWidth: 1f);

            Bind(dialog, ("titleLabel", title), ("messageLabel", message), ("primaryButton", primary), ("primaryLabel", primaryLabel),
                ("secondaryButton", secondary), ("secondaryLabel", secondaryLabel));
            return dialog;
        }

        // ------------------------------------------------------------ cena

        private static void SetupScene()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Abrir a cena descarrega os assets sem uso: carregar só depois, pelo caminho.
            var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            var catalog = AssetDatabase.LoadAssetAtPath<PartCatalog>(CatalogPath);
            var procedure = AssetDatabase.LoadAssetAtPath<AssemblyProcedure>(MontagemPreviewBuilder.ProcedurePath);
            if (uiPrefab == null || catalog == null || procedure == null)
                throw new InvalidOperationException("Prefab da interface, catálogo ou roteiro não encontrado depois de abrir a cena.");

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == UiRootName || rootObject.name == StageRootName)
                    Object.DestroyImmediate(rootObject);
            }

            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var module = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            DemoStage stage = BuildDemoStage();
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, scene);

            GameObject montar = GameObject.Find("MontAR");
            if (montar == null)
                throw new InvalidOperationException("A cena não tem o objeto 'MontAR' (rastreamento e overlay).");
            var tracking = montar.GetComponent<ImageTrackingController>();
            var content = montar.GetComponent<ARContentManager>();
            var hud = montar.GetComponent<PocDiagnosticsHud>();
            var logger = GetOrAdd<SessionMetricsLogger>(montar);
            var flow = GetOrAdd<StepFlowController>(montar);
            var app = GetOrAdd<AppController>(montar);

            // Peças virtuais na RA: a placa genérica presa ao marcador A, na mesma posição do tapete.
            MontagemPreviewSpec spec = MontagemPreviewBuilder.LoadSpec();
            var boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MontagemPreviewBuilder.BoardPrefabPath);
            Vector3 boardOffset = MontagemPreviewBuilder.MatToWorld(spec.mat, spec.mat.board_left_mm, spec.mat.board_top_mm, 0f)
                - MontagemPreviewBuilder.MarkerWorld(spec);
            Bind(content, ("previewStep", (Object)null), ("assemblyReferenceName", MontagemPreviewBuilder.MarkerName),
                ("virtualBoardPrefab", boardPrefab), ("virtualBoardOffset", boardOffset), ("installedMaterial", InstalledMaterial()));
            if (hud != null)
                hud.enabled = false;

            Camera arCamera = Object.FindAnyObjectByType<ARCameraManager>()?.GetComponent<Camera>();
            if (arCamera == null)
                throw new InvalidOperationException("Câmera da RA (ARCameraManager) não encontrada na cena.");
            // A sessão do ARCore pede a câmera ao ligar: começa desligada e o AppController liga
            // depois de explicar e pedir a permissão (ou na hora, se ela já foi dada).
            ARSession arSession = Object.FindAnyObjectByType<ARSession>(FindObjectsInactive.Include);
            if (arSession == null)
                throw new InvalidOperationException("ARSession não encontrada na cena.");
            arSession.enabled = false;
            EditorUtility.SetDirty(arSession);

            Bind(flow, ("procedure", procedure), ("imageTracking", tracking), ("contentManager", content), ("demoStage", stage), ("metricsLogger", logger));
            Bind(ui.GetComponentInChildren<DemoOrbitInput>(true), ("stage", stage));
            Bind(ui.GetComponentInChildren<AssemblyScreen>(true), ("demoStage", stage));
            Bind(app,
                ("flowController", flow), ("catalog", catalog), ("arCamera", arCamera), ("arSession", arSession), ("demoStage", stage),
                ("welcomeScreen", ui.GetComponentInChildren<WelcomeScreen>(true)),
                ("partsScreen", ui.GetComponentInChildren<PartsScreen>(true)),
                ("startScreen", ui.GetComponentInChildren<StartScreen>(true)),
                ("assemblyScreen", ui.GetComponentInChildren<AssemblyScreen>(true)),
                ("completionScreen", ui.GetComponentInChildren<CompletionScreen>(true)),
                ("historyScreen", ui.GetComponentInChildren<HistoryScreen>(true)),
                ("dialog", ui.GetComponentInChildren<ConfirmDialog>(true)),
                ("toast", ui.transform.Find("Aviso").GetComponent<Toast>()),
                ("diagnosticsHud", hud),
                ("diagnosticsToggle", ui.GetComponentInChildren<WelcomeScreen>(true).GetComponentInChildren<LongPressButton>(true)));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static DemoStage BuildDemoStage()
        {
            MontagemPreviewSpec spec = MontagemPreviewBuilder.LoadSpec();
            MatSpec mat = spec.mat;
            var materials = new MontagemPreviewBuilder.MaterialCache();

            var root = new GameObject(StageRootName);
            root.transform.position = new Vector3(0f, StageDepth, 0f);
            var stage = root.AddComponent<DemoStage>();

            var bench = new GameObject("Bancada virtual");
            bench.transform.SetParent(root.transform, false);

            GameObject desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Mesa";
            Object.DestroyImmediate(desk.GetComponent<Collider>());
            desk.transform.SetParent(bench.transform, false);
            desk.transform.localPosition = new Vector3(0f, -0.015f, 0f);
            desk.transform.localScale = new Vector3(Mathf.Max(1.4f, mat.width_mm / 1000f + 0.6f), 0.03f, Mathf.Max(1.0f, mat.height_mm / 1000f + 0.5f));
            desk.GetComponent<MeshRenderer>().sharedMaterial = materials.Lit(new Color(0.3f, 0.22f, 0.16f), false);

            GameObject matQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            matQuad.name = "Tapete de montagem";
            Object.DestroyImmediate(matQuad.GetComponent<Collider>());
            matQuad.transform.SetParent(bench.transform, false);
            matQuad.transform.localPosition = new Vector3(0f, 0.001f, 0f);
            matQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            matQuad.transform.localScale = new Vector3(mat.width_mm / 1000f, mat.height_mm / 1000f, 1f);
            var matTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MontagemPreviewBuilder.MatTexturePath);
            matQuad.GetComponent<MeshRenderer>().sharedMaterial = materials.Textured("Tapete", matTexture, 0.05f);

            var boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MontagemPreviewBuilder.BoardPrefabPath);
            var board = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, bench.transform);
            board.transform.localPosition = MontagemPreviewBuilder.MatToWorld(mat, mat.board_left_mm, mat.board_top_mm, 0f);

            var marker = new GameObject("Marcador A (virtual)");
            marker.transform.SetParent(bench.transform, false);
            marker.transform.localPosition = MontagemPreviewBuilder.MarkerWorld(spec);

            // Peças virtuais encaixadas: no marcador virtual, como o overlay (a bancada já tem a placa-mãe).
            var installed = new GameObject("Peças encaixadas (virtuais)");
            installed.transform.SetParent(marker.transform, false);
            var assembly = installed.AddComponent<VirtualAssembly>();
            Bind(assembly, ("installedMaterial", InstalledMaterial()));

            BoardElementSpec pcb = Array.Find(spec.board_elements, e => e.id == "pcb");
            var center = new GameObject("Centro da placa");
            center.transform.SetParent(bench.transform, false);
            center.transform.localPosition = MontagemPreviewBuilder.MatToWorld(mat, mat.board_left_mm + pcb.w_mm * 0.5f, mat.board_top_mm + pcb.h_mm * 0.5f, 0f);

            var cameraObject = new GameObject("Câmera da demonstração 3D");
            cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiTheme.Background;
            camera.depth = 1f;
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            // Fundo aurora atrás dos menus e da bancada. É desenhado pela própria câmera (Canvas em Screen
            // Space - Camera), então entra no desfoque do vidro. Maior que a tela: a bancada desloca a projeção.
            var backdrop = new GameObject("Fundo aurora", typeof(RectTransform));
            backdrop.layer = LayerMask.NameToLayer("UI");
            backdrop.transform.SetParent(cameraObject.transform, false);
            var backdropCanvas = backdrop.AddComponent<Canvas>();
            backdropCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            backdropCanvas.worldCamera = camera;
            backdropCanvas.planeDistance = 15f;
            backdropCanvas.sortingOrder = -100;
            RectTransform backdropImage = Stretch(Node("Imagem", backdrop.transform));
            backdropImage.localScale = Vector3.one * 1.3f;
            Image aurora = Img(backdropImage.gameObject, Color.white, LoadSprite("fundo_aurora"));
            aurora.raycastTarget = false;
            var fitter = backdropImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1080f / 1920f;

            Bind(stage, ("stageRoot", bench), ("markerAnchor", marker.transform), ("boardCenter", center.transform), ("stageCamera", camera),
                ("assembly", assembly));
            bench.SetActive(false);
            return stage;
        }

        /// <summary>Material de base das peças virtuais encaixadas (a cor de cada peça muda em tempo de execução).</summary>
        private static Material InstalledMaterial()
        {
            return new MontagemPreviewBuilder.MaterialCache().Lit(new Color(0.25f, 0.27f, 0.31f), false);
        }

        // ------------------------------------------------------------ utilitários

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        /// <summary>Preenche campos [SerializeField] privados pelo nome (falha alto se o nome não existir).</summary>
        public static void Bind(Object target, params (string field, object value)[] values)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var so = new SerializedObject(target);
            foreach ((string field, object value) in values)
            {
                SerializedProperty property = so.FindProperty(field);
                if (property == null)
                    throw new ArgumentException($"{target.GetType().Name} não tem o campo '{field}'.");

                switch (value)
                {
                    case float number:
                        property.floatValue = number;
                        break;
                    case int integer:
                        property.intValue = integer;
                        break;
                    case bool flag:
                        property.boolValue = flag;
                        break;
                    case string text:
                        property.stringValue = text;
                        break;
                    case Color color:
                        property.colorValue = color;
                        break;
                    case Vector3 vector:
                        property.vector3Value = vector;
                        break;
                    default:
                        property.objectReferenceValue = value as Object;
                        break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
