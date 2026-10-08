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

        private static void ConfigureUiSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesFolder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                bool rounded = Path.GetFileNameWithoutExtension(path) == "ui_arredondado";
                var border = rounded ? new Vector4(48f, 48f, 48f, 48f) : Vector4.zero;
                bool changed = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteBorder != border
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency;
                if (!changed)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteBorder = border;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------ catálogo

        private struct PartSpec
        {
            public string Id, Name, Description, Icon, UnitSingular, UnitPlural, ModelId, ModelName, ModelDetail;
            public int Min, Max, Default;
        }

        private static readonly PartSpec[] DefaultParts =
        {
            new PartSpec { Id = "placa_mae", Name = "Placa-mãe", Icon = "peca_placa_mae", UnitSingular = "placa", UnitPlural = "placas", Min = 1, Max = 1, Default = 1,
                Description = "Base da montagem: as outras peças encaixam nela. Fica deitada no tapete.",
                ModelId = "atx_generica_lga1700", ModelName = "ATX genérica · LGA1700 · DDR5",
                ModelDetail = "Modelo de demonstração, com posições aproximadas, até o kit do laboratório ser definido (D5)." },
            new PartSpec { Id = "cpu", Name = "Processador (CPU)", Icon = "peca_cpu", UnitSingular = "unidade", UnitPlural = "unidades", Min = 1, Max = 1, Default = 1,
                Description = "Vai no soquete; o triângulo dourado mostra o sentido.",
                ModelId = "intel_lga1700", ModelName = "Intel · soquete LGA1700" },
            new PartSpec { Id = "cooler", Name = "Cooler do processador", Icon = "peca_cooler", UnitSingular = "unidade", UnitPlural = "unidades", Min = 1, Max = 1, Default = 1,
                Description = "Tira o calor da CPU; o cabo vai no conector CPU_FAN.",
                ModelId = "cooler_ar_4pinos", ModelName = "Cooler a ar · cabo de 4 pinos" },
            new PartSpec { Id = "ram", Name = "Memória RAM", Icon = "peca_ram", UnitSingular = "pente", UnitPlural = "pentes", Min = 1, Max = 2, Default = 2,
                Description = "1 ou 2 pentes: o roteiro indica os slots certos para cada caso.",
                ModelId = "ddr5_dimm", ModelName = "DDR5 · pente DIMM" },
            new PartSpec { Id = "ssd", Name = "SSD M.2", Icon = "peca_ssd", UnitSingular = "unidade", UnitPlural = "unidades", Min = 0, Max = 1, Default = 1,
                Description = "Armazenamento: entra inclinado no conector e prende com parafuso.",
                ModelId = "ssd_m2_2280_nvme", ModelName = "M.2 2280 · NVMe" },
            new PartSpec { Id = "gpu", Name = "Placa de vídeo", Icon = "peca_gpu", UnitSingular = "unidade", UnitPlural = "unidades", Min = 0, Max = 1, Default = 1,
                Description = "Opcional: vai no slot PCIe x16 de cima.",
                ModelId = "gpu_pcie_x16", ModelName = "PCIe x16 · 1 cabo de energia PCIe" },
            new PartSpec { Id = "fonte", Name = "Fonte de alimentação", Icon = "peca_fonte", UnitSingular = "unidade", UnitPlural = "unidades", Min = 1, Max = 1, Default = 1,
                Description = "Liga a placa pelos cabos de 24 pinos e EPS 8 pinos.",
                ModelId = "fonte_atx", ModelName = "ATX · cabos 24 pinos, EPS 4+4 e PCIe 6+2" },
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
                models.arraySize = 1;
                SerializedProperty model = models.GetArrayElementAtIndex(0);
                model.FindPropertyRelative("modelId").stringValue = spec.ModelId;
                model.FindPropertyRelative("displayName").stringValue = spec.ModelName;
                model.FindPropertyRelative("detail").stringValue = spec.ModelDetail ?? string.Empty;
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
            Toast toast;
            AssemblyScreen assembly = BuildAssembly(screens);
            CompletionScreen completion = BuildCompletion(screens);
            toast = BuildToast(root.transform);
            ConfirmDialog dialog = BuildDialog(root.transform);
            Bind(assembly, ("toast", toast));

            foreach (UiScreen screen in new UiScreen[] { welcome, parts, start, assembly, completion })
                screen.gameObject.SetActive(false);
            dialog.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Tela de fundo inteiro com o conteúdo dentro da área segura.</summary>
        private static RectTransform ScreenRoot<T>(Transform parent, string name, Color? background, out T screen) where T : UiScreen
        {
            RectTransform root = Stretch(Node(name, parent));
            root.gameObject.AddComponent<CanvasGroup>();
            if (background.HasValue)
                Img(root.gameObject, background.Value);
            screen = root.gameObject.AddComponent<T>();

            RectTransform safe = Stretch(Node("Área segura", root));
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        private static WelcomeScreen BuildWelcome(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 1 - Apresentação", UiTheme.Background, out WelcomeScreen screen);
            VLayout(content.gameObject, Pad(64, 64, 36, 40), 0f, TextAnchor.UpperCenter);

            RectTransform top = Node("Topo", content);
            HLayout(top.gameObject, null, 16f, TextAnchor.MiddleLeft);
            Layout(top.gameObject, preferredHeight: 110f);
            TextMeshProUGUI logo = Label(top, "Logo", "Mont<color=#16A34A>AR</color>", 60f, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Layout(logo.gameObject, flexibleWidth: 1f);
            Button skip = MakeButton(top, "Botão Pular", "Pular", ButtonStyle.Ghost, out _, 88f, 36f);
            Layout(skip.gameObject, preferredHeight: 88f, preferredWidth: 200f);

            Spacer(content, 1f);
            RectTransform iconRow = Node("Linha do ícone", content);
            HLayout(iconRow.gameObject, null, 0f, TextAnchor.MiddleCenter);
            Image icon = Badge(iconRow, "Ícone da página", LoadSprite("icone_cubo"), UiTheme.PrimarySoft, UiTheme.Primary, 380f, 0.55f);
            Spacer(content, 0f, 64f);
            TextMeshProUGUI title = Label(content, "Título", "Bem-vindo ao MontAR", 68f, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Top);
            Spacer(content, 0f, 28f);
            TextMeshProUGUI body = Label(content, "Texto", "", 40f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Top);
            body.lineSpacing = 10f;
            Spacer(content, 1.2f);

            RectTransform dots = Node("Pontos", content);
            HLayout(dots.gameObject, null, 14f, TextAnchor.MiddleCenter, false, false);
            Layout(dots.gameObject, preferredHeight: 30f);
            RectTransform dot = Node("Ponto", dots);
            dot.sizeDelta = new Vector2(18f, 18f);
            Image dotImage = Card(dot.gameObject, UiTheme.Border, 9f);
            dotImage.raycastTarget = false;
            Spacer(content, 0f, 44f);

            RectTransform buttons = Node("Botões", content);
            HLayout(buttons.gameObject, null, 24f, TextAnchor.MiddleCenter);
            Layout(buttons.gameObject, preferredHeight: 132f);
            Button back = MakeButton(buttons, "Botão Voltar", "Voltar", ButtonStyle.Secondary, out _, 132f);
            Layout(back.gameObject, preferredHeight: 132f, preferredWidth: 300f);
            Button next = MakeButton(buttons, "Botão Próximo", "Próximo", ButtonStyle.Primary, out TextMeshProUGUI nextLabel, 132f);
            Layout(next.gameObject, preferredHeight: 132f, flexibleWidth: 1f);

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
                ("Bem-vindo ao MontAR", "Monte um computador de verdade com a instrução em 3D em cima da peça real. O app guia uma etapa por vez e só libera a próxima depois que você confirma a anterior.", "icone_cubo"),
                ("1. Aponta", "Aponte a câmera do celular para o marcador A do tapete de montagem. É por ele que o app sabe onde fica cada encaixe da placa-mãe.", "icone_aponta"),
                ("2. Guia", "Uma peça fantasma em 3D mostra onde e em que sentido encaixar. Faça o mesmo movimento com a peça de verdade. Sem câmera? Toque em 3D e veja tudo numa bancada virtual.", "icone_guia"),
                ("3. Valida", "Terminou a etapa? Toque em Validar e confira a lista. Se algo não bate, a etapa volta para você corrigir antes de seguir.", "icone_valida"),
                ("4. Registra", "Cada etapa validada fica salva. Se o app fechar, a montagem continua de onde parou.", "icone_registra"),
                ("Antes de começar", "Tapete impresso sobre a mesa, com o marcador A à vista. Peças separadas ao lado e a fonte fora da tomada. Luz boa e o celular na mão.", "icone_camera"),
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
            RectTransform header = Node("Cabeçalho", content);
            VLayout(header.gameObject, Pad(56, 56, 20, 24), 10f);
            RectTransform backRow = Node("Linha do voltar", header);
            HLayout(backRow.gameObject, null, 0f, TextAnchor.MiddleLeft);
            Layout(backRow.gameObject, preferredHeight: 88f);
            back = MakeButton(backRow, "Botão Voltar", "‹ Voltar", ButtonStyle.Ghost, out TextMeshProUGUI backLabel, 88f, 36f);
            backLabel.alignment = TextAlignmentOptions.MidlineLeft;
            backLabel.rectTransform.offsetMin = new Vector2(0f, 0f);
            Layout(back.gameObject, preferredHeight: 88f, preferredWidth: 220f);
            Label(header, "Título", title, 62f, UiTheme.TextPrimary, FontStyles.Bold);
            TextMeshProUGUI sub = Label(header, "Subtítulo", subtitle, 34f, UiTheme.TextSecondary);
            sub.lineSpacing = 8f;
            return header;
        }

        private static RectTransform Footer(Transform content)
        {
            RectTransform footer = Node("Rodapé", content);
            Img(footer.gameObject, UiTheme.Surface);
            VLayout(footer.gameObject, Pad(56, 56, 24, 36), 14f);
            return footer;
        }

        private static PartsScreen BuildParts(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 2 - Peças", UiTheme.Background, out PartsScreen screen);
            VLayout(content.gameObject, null, 0f);
            Header(content, "Escolha as peças", "Por enquanto há uma opção de cada componente. O roteiro se ajusta ao que você marcar.", out Button back);

            RectTransform list = Scroll(content, "Lista de peças", Pad(40, 40, 8, 40), 22f, out ScrollRect scroll);
            PartRowView row = BuildPartRow(list);

            RectTransform footer = Footer(content);
            TextMeshProUGUI summary = Label(footer, "Resumo", "O roteiro terá 9 etapas", 34f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center);
            Button next = MakeButton(footer, "Botão Continuar", "Continuar", ButtonStyle.Primary, out _, 132f);

            Bind(screen, ("listContainer", list), ("rowTemplate", row), ("summaryLabel", summary), ("continueButton", next),
                ("backButton", back), ("scrollRect", scroll));
            return screen;
        }

        private static PartRowView BuildPartRow(Transform parent)
        {
            RectTransform row = Node("Peça (modelo)", parent);
            Card(row.gameObject, UiTheme.Surface, 36f);
            HLayout(row.gameObject, Pad(32, 28, 32, 32), 20f, TextAnchor.MiddleLeft);

            RectTransform dimmed = Node("Peça", row);
            HLayout(dimmed.gameObject, null, 28f, TextAnchor.MiddleLeft);
            Layout(dimmed.gameObject, flexibleWidth: 1f);
            CanvasGroup group = dimmed.gameObject.AddComponent<CanvasGroup>();
            Image icon = Badge(dimmed, "Ícone", null, UiTheme.PrimarySoft, UiTheme.Primary, 124f, 0.64f);

            RectTransform texts = Node("Textos", dimmed);
            VLayout(texts.gameObject, null, 6f);
            Layout(texts.gameObject, flexibleWidth: 1f);
            TextMeshProUGUI name = Label(texts, "Nome", "Peça", 40f, UiTheme.TextPrimary, FontStyles.Bold);
            RectTransform modelRow = Node("Modelo", texts);
            HLayout(modelRow.gameObject, null, 8f, TextAnchor.MiddleLeft);
            Button previous = IconButton(modelRow, "Modelo anterior", "‹", null, UiTheme.SurfaceMuted, UiTheme.TextPrimary, 56f, 40f, out _);
            TextMeshProUGUI model = Label(modelRow, "Nome do modelo", "Modelo", 30f, UiTheme.Primary, FontStyles.Bold);
            Layout(model.gameObject, flexibleWidth: 1f);
            Button nextModel = IconButton(modelRow, "Próximo modelo", "›", null, UiTheme.SurfaceMuted, UiTheme.TextPrimary, 56f, 40f, out _);
            TextMeshProUGUI description = Label(texts, "Descrição", "", 28f, UiTheme.TextMuted);
            TextMeshProUGUI requirement = Label(texts, "Obrigatória ou opcional", "obrigatória", 24f, UiTheme.TextMuted, FontStyles.Bold | FontStyles.UpperCase);

            RectTransform stepper = Node("Quantidade", row);
            VLayout(stepper.gameObject, null, 10f, TextAnchor.MiddleCenter, true, false);
            Layout(stepper.gameObject, preferredWidth: 200f, minWidth: 200f, flexibleWidth: 0f);
            RectTransform buttons = Node("Botões", stepper);
            HLayout(buttons.gameObject, null, 18f, TextAnchor.MiddleCenter);
            Button minus = IconButton(buttons, "Menos", "–", null, UiTheme.SurfaceMuted, UiTheme.TextPrimary, 84f, 48f, out _);
            Button plus = IconButton(buttons, "Mais", "+", null, UiTheme.SurfaceMuted, UiTheme.TextPrimary, 84f, 48f, out _);
            CanvasGroup buttonsGroup = buttons.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI quantity = Label(stepper, "Quantidade escolhida", "1 unidade", 28f, UiTheme.TextSecondary, FontStyles.Bold, TextAlignmentOptions.Center);

            var view = row.gameObject.AddComponent<PartRowView>();
            Bind(view, ("icon", icon), ("nameLabel", name), ("modelLabel", model), ("descriptionLabel", description),
                ("requirementLabel", requirement), ("previousModelButton", previous), ("nextModelButton", nextModel),
                ("minusButton", minus), ("plusButton", plus), ("quantityLabel", quantity), ("quantityButtons", buttonsGroup), ("dimmedGroup", group));
            return view;
        }

        private static RectTransform CardSection(Transform parent, string name, string title, out TextMeshProUGUI titleLabel)
        {
            RectTransform card = Node(name, parent);
            Card(card.gameObject, UiTheme.Surface, 36f);
            VLayout(card.gameObject, Pad(40, 40, 36, 40), 18f);
            titleLabel = Label(card, "Título", title, 38f, UiTheme.TextPrimary, FontStyles.Bold);
            return card;
        }

        private static StartScreen BuildStart(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 3 - Início", UiTheme.Background, out StartScreen screen);
            VLayout(content.gameObject, null, 0f);
            Header(content, "Tudo pronto?", "Informe o código do participante e escolha como ver a instrução.", out Button back);

            RectTransform scrollContent = Scroll(content, "Opções", Pad(40, 40, 8, 40), 24f, out _);

            RectTransform participant = CardSection(scrollContent, "Participante", "Código do participante", out _);
            TMP_InputField input = TextInput(participant, "Campo do código", "P01", out _);
            Label(participant, "Dica", "Código anônimo, como P01. Nunca use nome ou RA.", 28f, UiTheme.TextMuted);
            TextMeshProUGUI error = Label(participant, "Erro", "", 28f, UiTheme.Danger, FontStyles.Bold);
            error.gameObject.SetActive(false);

            RectTransform modeCard = CardSection(scrollContent, "Modo", "Como ver a instrução", out _);
            RectTransform segmented = Node("Escolha do modo", modeCard);
            Card(segmented.gameObject, UiTheme.SurfaceMuted, 34f);
            HLayout(segmented.gameObject, Pad(8), 8f, TextAnchor.MiddleCenter);
            Layout(segmented.gameObject, preferredHeight: 124f);
            Button ar = MakeButton(segmented, "Modo RA", "Câmera (RA)", ButtonStyle.Primary, out TextMeshProUGUI arLabel, 108f, 34f);
            Layout(ar.gameObject, preferredHeight: 108f, flexibleWidth: 1f);
            Button demo = MakeButton(segmented, "Modo 3D", "Demonstração 3D", ButtonStyle.Secondary, out TextMeshProUGUI demoLabel, 108f, 34f);
            Layout(demo.gameObject, preferredHeight: 108f, flexibleWidth: 1f);
            TextMeshProUGUI note = Label(modeCard, "Explicação do modo", "", 30f, UiTheme.TextSecondary);
            note.lineSpacing = 6f;

            RectTransform stepsCard = CardSection(scrollContent, "Roteiro", "Roteiro", out TextMeshProUGUI stepsHeader);
            TextMeshProUGUI steps = Label(stepsCard, "Etapas", "", 32f, UiTheme.TextSecondary);
            steps.lineSpacing = 22f;

            RectTransform footer = Footer(content);
            Button startButton = MakeButton(footer, "Botão Iniciar", "Iniciar montagem", ButtonStyle.Primary, out _, 132f);

            Bind(screen, ("participantInput", input), ("participantError", error),
                ("modeArButton", ar), ("modeArBackground", ar.targetGraphic), ("modeArLabel", arLabel),
                ("modeDemoButton", demo), ("modeDemoBackground", demo.targetGraphic), ("modeDemoLabel", demoLabel),
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

            // Topo: etapa, componente, progresso, sair e o botão discreto do observador.
            RectTransform top = TopBand(Node("Topo", safe), 24f, 214f, 24f, 24f);
            Card(top.gameObject, UiTheme.SurfaceDark, 40f);
            VLayout(top.gameObject, Pad(30, 30, 26, 30), 20f);
            RectTransform row = Node("Linha", top);
            HLayout(row.gameObject, null, 24f, TextAnchor.MiddleLeft);
            Layout(row.gameObject, preferredHeight: 118f);
            Button exit = IconButton(row, "Botão Sair", "×", null, new Color(1f, 1f, 1f, 0.12f), UiTheme.TextOnDark, 92f, 60f, out _);
            RectTransform texts = Node("Textos", row);
            VLayout(texts.gameObject, null, 2f, TextAnchor.MiddleLeft);
            Layout(texts.gameObject, flexibleWidth: 1f);
            TextMeshProUGUI counter = Label(texts, "Etapa", "Etapa 1 de 9", 46f, UiTheme.TextOnDark, FontStyles.Bold);
            TextMeshProUGUI component = Label(texts, "Componente", "Componente", 30f, UiTheme.TextOnDarkMuted);
            component.textWrappingMode = TextWrappingModes.NoWrap;
            component.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform observer = Node("Observador (toque longo)", row);
            Layout(observer.gameObject, preferredHeight: 60f, preferredWidth: 60f, minHeight: 60f, minWidth: 60f);
            Img(observer.gameObject, new Color(1f, 1f, 1f, 0.14f), Circle);
            RectTransform fill = Stretch(Node("Progresso", observer));
            Image fillImage = Img(fill.gameObject, UiTheme.Primary, Circle);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
            var longPress = observer.gameObject.AddComponent<LongPressButton>();
            Bind(longPress, ("holdSeconds", 1.2f), ("progressFill", fillImage));

            RectTransform bar = Node("Barra de progresso", top);
            Layout(bar.gameObject, preferredHeight: 14f);
            Card(bar.gameObject, new Color(1f, 1f, 1f, 0.16f), 7f);
            RectTransform barFill = Stretch(Node("Preenchimento", bar));
            Image progress = Img(barFill.gameObject, UiTheme.Primary, Rounded, 48f / 7f);
            progress.type = Image.Type.Filled;
            progress.fillMethod = Image.FillMethod.Horizontal;
            progress.fillAmount = 0.1f;

            // Situação da etapa e troca de modo.
            RectTransform status = TopBand(Node("Situação e modo", safe), 258f, 84f, 24f, 24f);
            HLayout(status.gameObject, null, 14f, TextAnchor.MiddleLeft);
            RectTransform chip = Node("Situação da etapa", status);
            Image chipImage = Card(chip.gameObject, UiTheme.StatusLocating, 42f);
            Layout(chip.gameObject, preferredHeight: 84f, flexibleWidth: 1f, minWidth: 0f, preferredWidth: 0f);
            TextMeshProUGUI statusLabel = Label(chip, "Texto", "Aponte a câmera para o marcador A", 29f, UiTheme.TextOnDark, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(statusLabel.rectTransform, 32f, 0f, 24f, 0f);
            statusLabel.textWrappingMode = TextWrappingModes.Normal;
            statusLabel.enableAutoSizing = true;
            statusLabel.fontSizeMin = 22f;
            statusLabel.fontSizeMax = 29f;

            RectTransform modes = Node("Modo", status);
            Card(modes.gameObject, UiTheme.SurfaceDark, 42f);
            HLayout(modes.gameObject, Pad(6), 6f, TextAnchor.MiddleCenter);
            Layout(modes.gameObject, preferredHeight: 84f, preferredWidth: 360f, flexibleWidth: 0f, minWidth: 360f);
            Button ar = MakeButton(modes, "Modo RA", "Câmera", ButtonStyle.Ghost, out TextMeshProUGUI arLabel, 72f, 28f);
            Layout(ar.gameObject, preferredHeight: 72f, flexibleWidth: 1f);
            Button demo = MakeButton(modes, "Modo 3D", "3D", ButtonStyle.Ghost, out TextMeshProUGUI demoLabel, 72f, 28f);
            Layout(demo.gameObject, preferredHeight: 72f, flexibleWidth: 1f);
            arLabel.color = UiTheme.TextOnDark;
            demoLabel.color = UiTheme.TextOnDark;

            RectTransform hint = TopBand(Node("Dica da demonstração 3D", safe), 358f, 64f, 24f, 24f);
            HLayout(hint.gameObject, null, 0f, TextAnchor.MiddleCenter, false, false);
            RectTransform hintPill = Node("Fundo", hint);
            hintPill.sizeDelta = new Vector2(940f, 64f);
            Card(hintPill.gameObject, new Color(0f, 0f, 0f, 0.45f), 32f).raycastTarget = false;
            TextMeshProUGUI hintLabel = Label(hintPill, "Texto", "Arraste para girar · pinça para aproximar · toque duplo centraliza", 26f, UiTheme.TextOnDark, FontStyles.Normal, TextAlignmentOptions.Center);
            Stretch(hintLabel.rectTransform, 20f, 0f, 20f, 0f);
            hintLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // Painel da etapa (Guia).
            RectTransform sheet = BottomBand(Node("Painel da etapa", safe), 24f, 24f, 24f);
            Card(sheet.gameObject, UiTheme.Surface, 44f);
            VLayout(sheet.gameObject, Pad(44, 44, 10, 40), 18f);
            FitHeight(sheet.gameObject);

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
            Card(grabber.gameObject, UiTheme.Border, 5f).raycastTarget = false;
            Layout(grabber.gameObject, preferredHeight: 10f, preferredWidth: 110f);
            TextMeshProUGUI collapseLabel = Label(handle, "Texto", "Esconder instrução", 24f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            Layout(collapseLabel.gameObject, preferredHeight: 34f);

            TextMeshProUGUI title = Label(sheet, "Título", "Título da etapa", 46f, UiTheme.TextPrimary, FontStyles.Bold);
            RectTransform details = Node("Detalhes", sheet);
            VLayout(details.gameObject, null, 20f);
            TextMeshProUGUI instruction = Label(details, "Instrução", "", 34f, UiTheme.TextSecondary);
            instruction.lineSpacing = 8f;
            RectTransform alert = Node("Alerta", details);
            Card(alert.gameObject, UiTheme.AlertBackground, 28f);
            VLayout(alert.gameObject, Pad(30, 30, 24, 26), 0f);
            TextMeshProUGUI alertLabel = Label(alert, "Texto", "", 31f, UiTheme.AlertText);
            alertLabel.lineSpacing = 6f;
            Button validate = MakeButton(sheet, "Botão Validar", "Validar etapa", ButtonStyle.Primary, out TextMeshProUGUI validateLabel, 128f);

            // Painel da validação (Valida).
            RectTransform validation = BottomBand(Node("Painel da validação", safe), 24f, 24f, 24f);
            Card(validation.gameObject, UiTheme.Surface, 44f);
            VLayout(validation.gameObject, Pad(44, 44, 40, 40), 22f);
            FitHeight(validation.gameObject);
            Label(validation, "Título", "Confira antes de avançar", 46f, UiTheme.TextPrimary, FontStyles.Bold);
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

            Bind(screen,
                ("exitButton", exit), ("stepCounter", counter), ("componentLabel", component), ("progressFill", progress), ("observerButton", longPress),
                ("statusChip", chipImage), ("statusLabel", statusLabel),
                ("modeArButton", ar), ("modeArBackground", ar.targetGraphic), ("modeArLabel", arLabel),
                ("modeDemoButton", demo), ("modeDemoBackground", demo.targetGraphic), ("modeDemoLabel", demoLabel),
                ("stepSheet", sheet.gameObject), ("collapseButton", collapse), ("collapseLabel", collapseLabel), ("titleLabel", title),
                ("detailsGroup", details.gameObject), ("instructionLabel", instruction), ("alertBox", alert.gameObject), ("alertLabel", alertLabel),
                ("validateButton", validate), ("validateLabel", validateLabel),
                ("validationSheet", validation.gameObject), ("validationHint", validationHint), ("checklistContainer", checklist),
                ("checklistTemplate", item), ("confirmButton", confirm), ("confirmLabel", confirmLabel), ("cancelButton", cancel),
                ("orbitSurface", orbit.gameObject), ("demoHint", hint.gameObject));
            return screen;
        }

        private static CompletionScreen BuildCompletion(Transform parent)
        {
            RectTransform content = ScreenRoot(parent, "Tela 5 - Conclusão", UiTheme.Background, out CompletionScreen screen);
            VLayout(content.gameObject, null, 0f);

            RectTransform scroll = Scroll(content, "Resumo", Pad(56, 56, 48, 32), 24f, out _);
            RectTransform badgeRow = Node("Linha do ícone", scroll);
            HLayout(badgeRow.gameObject, null, 0f, TextAnchor.MiddleCenter);
            Badge(badgeRow, "Concluído", LoadSprite("ui_check"), UiTheme.Primary, Color.white, 200f, 0.56f);
            Label(scroll, "Título", "Montagem concluída!", 62f, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Top);
            TextMeshProUGUI subtitle = Label(scroll, "Subtítulo", "", 34f, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Top);

            RectTransform grid = Node("Números", scroll);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(472f, 196f);
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
            Button again = MakeButton(footer, "Botão Nova montagem", "Nova montagem", ButtonStyle.Primary, out _, 128f);
            Button home = MakeButton(footer, "Botão Início", "Voltar ao início", ButtonStyle.Secondary, out _, 112f);

            Bind(screen, ("subtitleLabel", subtitle), ("timeValue", time), ("stepsValue", steps), ("failuresValue", failures),
                ("interventionsValue", interventions), ("stepTimesList", times), ("fileLabel", file),
                ("newSessionButton", again), ("homeButton", home));
            return screen;
        }

        private static TextMeshProUGUI Stat(Transform grid, string label)
        {
            RectTransform card = Node(label, grid);
            Card(card.gameObject, UiTheme.Surface, 36f);
            VLayout(card.gameObject, Pad(24, 24, 30, 24), 4f, TextAnchor.MiddleCenter);
            TextMeshProUGUI value = Label(card, "Valor", "0", 64f, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
            Label(card, "Rótulo", label, 27f, UiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
            return value;
        }

        private static Toast BuildToast(Transform parent)
        {
            RectTransform band = TopBand(Node("Aviso", parent), 470f, 120f, 80f, 80f);
            band.gameObject.AddComponent<CanvasGroup>();
            Card(band.gameObject, new Color(0.09f, 0.09f, 0.1f, 0.92f), 36f).raycastTarget = false;
            VLayout(band.gameObject, Pad(36, 36, 22, 22), 0f, TextAnchor.MiddleCenter);
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
            card.sizeDelta = new Vector2(940f, 400f);
            Card(card.gameObject, UiTheme.Surface, 44f);
            VLayout(card.gameObject, Pad(52, 52, 52, 44), 26f);
            FitHeight(card.gameObject);
            TextMeshProUGUI title = Label(card, "Título", "Título", 48f, UiTheme.TextPrimary, FontStyles.Bold);
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

            Bind(content, ("previewStep", (Object)null));
            if (hud != null)
                hud.enabled = false;

            Camera arCamera = Object.FindAnyObjectByType<ARCameraManager>()?.GetComponent<Camera>();
            if (arCamera == null)
                throw new InvalidOperationException("Câmera da RA (ARCameraManager) não encontrada na cena.");

            Bind(flow, ("procedure", procedure), ("imageTracking", tracking), ("contentManager", content), ("demoStage", stage), ("metricsLogger", logger));
            Bind(ui.GetComponentInChildren<DemoOrbitInput>(true), ("stage", stage));
            Bind(app,
                ("flowController", flow), ("catalog", catalog), ("arCamera", arCamera), ("demoStage", stage),
                ("welcomeScreen", ui.GetComponentInChildren<WelcomeScreen>(true)),
                ("partsScreen", ui.GetComponentInChildren<PartsScreen>(true)),
                ("startScreen", ui.GetComponentInChildren<StartScreen>(true)),
                ("assemblyScreen", ui.GetComponentInChildren<AssemblyScreen>(true)),
                ("completionScreen", ui.GetComponentInChildren<CompletionScreen>(true)),
                ("dialog", ui.GetComponentInChildren<ConfirmDialog>(true)),
                ("toast", ui.GetComponentInChildren<Toast>(true)),
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
            desk.GetComponent<MeshRenderer>().sharedMaterial = materials.Lit(new Color(0.55f, 0.38f, 0.24f), false);

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

            Bind(stage, ("stageRoot", bench), ("markerAnchor", marker.transform), ("boardCenter", center.transform), ("stageCamera", camera));
            bench.SetActive(false);
            return stage;
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
