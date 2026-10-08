using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MontAR.Dev
{
    /// <summary>
    /// Monta no Editor o conteúdo da montagem em bancada descrito em spec_montagem.json:
    /// - placa-mãe ATX genérica feita de primitivas (usada na prévia e na demonstração 3D do app);
    /// - um overlay por etapa (prefab em Assets/Prefabs/Overlays, usado pelo app na RA e na demonstração 3D);
    /// - as etapas (AssemblyStep) com o offset do alvo em relação ao marcador A e a peça de que dependem;
    /// - o roteiro (AssemblyProcedure) com todas as etapas na ordem do spec;
    /// - uma cena de prévia com mesa, tapete, placa e o marcador "rastreado", com os overlays como filhos
    ///   do marcador, do mesmo jeito que o ARContentManager faz no celular.
    /// Também renderiza imagens e quadros de animação pela câmera "do celular".
    /// Posições da placa são aproximadas (kit real ainda indefinido, decisão D5).
    /// </summary>
    public static class MontagemPreviewBuilder
    {
        public const string Root = "Assets/Dev/PreviewOverlay/";
        public const string SpecPath = Root + "spec_montagem.json";
        public const string ScenePath = Root + "Preview_Montagem.unity";
        public const string TrackedImageObjectName = "Imagem rastreada (simulada) - " + MarkerName;
        public const string CameraObjectName = "Camera do celular (prévia)";

        public const string BoardPrefabPath = "Assets/Prefabs/Bancada/PlacaMae_ATX_Generica.prefab";
        public const string BoardMaterialsFolder = "Assets/Materials/Bancada";
        public const string MatTexturePath = "Assets/Textures/Bancada/Tapete_montagem_demo.png";
        public const string ProcedurePath = "Assets/Data/Roteiro_bancada_v0.asset";
        public const string ProcedureId = "bancada_v0";
        public const string MarkerName = "MontAR_Marcador_A";
        private const string StepsFolder = "Assets/Data/Steps/Bancada";
        private const string SilkscreenTexturePath = "Assets/Textures/Bancada/PCB_serigrafia.png";
        private const string OverlayMaterialsFolder = "Assets/Materials/Overlays";
        private const string OverlayPrefabsFolder = "Assets/Prefabs/Overlays";
        private const string MeshFolder = "Assets/Models/Overlays";
        private const float MatTop = 0.001f;
        private const float DefaultPcbThicknessMm = 1.6f;

        // ---------------------------------------------------------------- menus

        [MenuItem("MontAR/Prévia/Reconstruir cena de montagem")]
        private static void RebuildMenu()
        {
            Debug.Log("[MontAR] " + Rebuild());
        }

        [MenuItem("MontAR/Prévia/Renderizar imagens (Builds/Previews)")]
        private static void RenderMenu()
        {
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Previews"));
            Debug.Log("[MontAR] " + RenderShots(outDir, 720, 1560, 0.55f));
        }

        // ---------------------------------------------------------------- construção

        /// <summary>Reconstrói prefabs, etapas demo e a cena a partir do spec. Devolve um resumo.</summary>
        public static string Rebuild()
        {
            MontagemPreviewSpec spec = LoadSpec();
            EnsureFolder(BoardMaterialsFolder);
            EnsureFolder(StepsFolder);
            EnsureFolder(OverlayMaterialsFolder);
            EnsureFolder(OverlayPrefabsFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder(Path.GetDirectoryName(BoardPrefabPath).Replace('\\', '/'));

            var materials = new MaterialCache();
            Mesh cone = EnsureMesh(MeshFolder + "/Overlay_Cone.asset", BuildConeMesh);
            Mesh triangle = EnsureMesh(MeshFolder + "/Overlay_TrianguloRetangulo.asset", BuildRightTrianglePrismMesh);

            GameObject boardPrefab = BuildBoardPrefab(spec, materials);

            var overlayPrefabs = new Dictionary<string, GameObject>();
            foreach (StepPreviewSpec step in spec.steps)
                overlayPrefabs[step.step_id] = step.overlay != null && step.overlay.Length > 0 ? BuildOverlayPrefab(step, materials, cone, triangle) : null;

            Vector3 markerWorld = MarkerWorld(spec);
            var stepAssets = new List<AssemblyStep>();
            foreach (StepPreviewSpec step in spec.steps)
                stepAssets.Add(BuildStepAsset(spec, step, overlayPrefabs[step.step_id], markerWorld));
            BuildProcedureAsset(stepAssets);

            BuildScene(spec, boardPrefab, stepAssets, materials, markerWorld);
            AssetDatabase.SaveAssets();

            var summary = new StringBuilder();
            summary.Append($"Conteúdo reconstruído: {spec.board_elements.Length} elementos na placa, {spec.steps.Length} etapas, roteiro {ProcedurePath}, cena {ScenePath}.");
            foreach (AssemblyStep step in stepAssets)
                summary.Append($" | {step.StepId}: offset {step.OverlayPositionOffset.ToString("F3")} m");
            return summary.ToString();
        }

        public static MontagemPreviewSpec LoadSpec()
        {
            if (!File.Exists(SpecPath))
                throw new FileNotFoundException("Spec da prévia não encontrado.", SpecPath);

            MontagemPreviewSpec spec = JsonUtility.FromJson<MontagemPreviewSpec>(File.ReadAllText(SpecPath));
            if (spec == null || spec.board_elements == null || spec.mat == null || spec.steps == null || spec.shots == null)
                throw new InvalidDataException("Spec da prévia incompleto: faltam board_elements, mat, steps ou shots.");
            return spec;
        }

        private static GameObject BuildBoardPrefab(MontagemPreviewSpec spec, MaterialCache materials)
        {
            var root = new GameObject("PlacaMae_ATX_Generica");
            BoardElementSpec pcb = Array.Find(spec.board_elements, e => e.id == "pcb");
            float pcbThickness = pcb != null && pcb.height_mm > 0f ? pcb.height_mm : DefaultPcbThicknessMm;

            foreach (BoardElementSpec element in spec.board_elements)
            {
                bool isPcb = element.id == "pcb";
                float height = Mathf.Max(element.height_mm, 0.2f);
                GameObject part = CreatePrimitive(PrimitiveType.Cube, string.IsNullOrEmpty(element.label_pt) ? element.id : $"{element.id} ({element.label_pt})");
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = BoardToLocal(element.x_mm, element.y_mm, isPcb ? height * 0.5f : pcbThickness + height * 0.5f);
                part.transform.localScale = new Vector3(Mathf.Max(element.w_mm, 0.5f), height, Mathf.Max(element.h_mm, 0.5f)) / 1000f;
                part.GetComponent<MeshRenderer>().sharedMaterial = materials.Lit(ToColor(element.color_rgb, 1f), element.metallic);
            }

            Texture2D silkscreen = AssetDatabase.LoadAssetAtPath<Texture2D>(SilkscreenTexturePath);
            if (silkscreen != null && pcb != null)
            {
                GameObject face = CreatePrimitive(PrimitiveType.Quad, "Serigrafia (PCB)");
                face.transform.SetParent(root.transform, false);
                face.transform.localPosition = BoardToLocal(pcb.x_mm, pcb.y_mm, pcbThickness + 0.02f);
                face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                face.transform.localScale = new Vector3(pcb.w_mm / 1000f, pcb.h_mm / 1000f, 1f);
                face.GetComponent<MeshRenderer>().sharedMaterial = materials.Textured("Placa_Serigrafia", silkscreen, 0.35f);
            }

            if (spec.mounting_holes != null)
            {
                Material pad = materials.Lit(new Color(0.78f, 0.66f, 0.32f), true);
                Material hole = materials.Lit(new Color(0.05f, 0.05f, 0.05f), false);
                for (int i = 0; i < spec.mounting_holes.Length; i++)
                {
                    MountingHoleSpec h = spec.mounting_holes[i];
                    GameObject ring = CreatePrimitive(PrimitiveType.Cylinder, $"Furo de fixação {i + 1}");
                    ring.transform.SetParent(root.transform, false);
                    ring.transform.localPosition = BoardToLocal(h.x_mm, h.y_mm, pcbThickness + 0.05f);
                    ring.transform.localScale = new Vector3(0.0075f, 0.00005f, 0.0075f);
                    ring.GetComponent<MeshRenderer>().sharedMaterial = pad;

                    GameObject center = CreatePrimitive(PrimitiveType.Cylinder, "Furo");
                    center.transform.SetParent(ring.transform, false);
                    center.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
                    center.GetComponent<MeshRenderer>().sharedMaterial = hole;
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildOverlayPrefab(StepPreviewSpec step, MaterialCache materials, Mesh cone, Mesh triangle)
        {
            var root = new GameObject("Overlay_" + step.step_id);
            foreach (OverlayElementSpec element in step.overlay)
            {
                GameObject part = BuildShape(element, materials, cone, triangle);
                part.transform.SetParent(root.transform, false);
                Vector3 rest = OffsetToLocal(element.offset_x_mm, element.offset_y_mm, element.offset_z_mm);
                part.transform.localPosition = rest;
                part.transform.localRotation = Quaternion.Euler(0f, ShapeYaw(element), 0f);

                OverlayMotionMode? mode = ParseMotion(element.motion);
                if (mode.HasValue)
                {
                    var motion = part.AddComponent<OverlayMotion>();
                    float period = element.period_s > 0f ? element.period_s : 2.2f;
                    float hold = element.hold_fraction > 0f ? element.hold_fraction : 0.35f;
                    Vector3 travel = OffsetToLocal(element.travel_x_mm, element.travel_y_mm, element.travel_z_mm);
                    motion.Configure(mode.Value, travel, period, hold, rest);
                }

                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{OverlayPrefabsFolder}/Overlay_{step.step_id}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildShape(OverlayElementSpec element, MaterialCache materials, Mesh cone, Mesh triangle)
        {
            Material material = materials.Overlay(ToColor(element.color_rgba, element.color_rgba != null && element.color_rgba.Length > 3 ? element.color_rgba[3] : 1f));
            float sx = Mathf.Max(element.size_x_mm, 0.5f) / 1000f;
            float sy = Mathf.Max(element.size_y_mm, 0.5f) / 1000f;
            float sz = Mathf.Max(element.size_z_mm, 0.3f) / 1000f;
            var group = new GameObject($"{element.id} [{element.shape}]");

            switch (element.shape)
            {
                case "losango":
                {
                    // Quadrado girado 45° dentro de um pai com escala não uniforme = losango com diagonais sx e sy.
                    var holder = new GameObject("Losango");
                    holder.transform.SetParent(group.transform, false);
                    holder.transform.localScale = new Vector3(sx, sz, sy);
                    GameObject square = CreatePrimitive(PrimitiveType.Cube, "Quadrado");
                    square.transform.SetParent(holder.transform, false);
                    square.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                    square.transform.localScale = new Vector3(0.7071f, 1f, 0.7071f);
                    square.GetComponent<MeshRenderer>().sharedMaterial = material;
                    break;
                }
                case "contorno_retangular":
                {
                    float bar = Mathf.Clamp(Mathf.Min(sx, sy) * 0.06f, 0.0012f, 0.003f);
                    AddBox(group, "Lado superior", new Vector3(0f, 0f, sy * 0.5f - bar * 0.5f), new Vector3(sx, sz, bar), material);
                    AddBox(group, "Lado inferior", new Vector3(0f, 0f, -sy * 0.5f + bar * 0.5f), new Vector3(sx, sz, bar), material);
                    AddBox(group, "Lado esquerdo", new Vector3(-sx * 0.5f + bar * 0.5f, 0f, 0f), new Vector3(bar, sz, sy - 2f * bar), material);
                    AddBox(group, "Lado direito", new Vector3(sx * 0.5f - bar * 0.5f, 0f, 0f), new Vector3(bar, sz, sy - 2f * bar), material);
                    break;
                }
                case "seta_para_baixo":
                {
                    float diameter = Mathf.Max(sx, sy);
                    float headHeight = Mathf.Min(sz * 0.45f, diameter * 1.1f);
                    float shaftHeight = Mathf.Max(sz - headHeight, 0.001f);
                    GameObject shaft = CreatePrimitive(PrimitiveType.Cylinder, "Haste");
                    shaft.transform.SetParent(group.transform, false);
                    shaft.transform.localPosition = new Vector3(0f, sz * 0.5f - shaftHeight * 0.5f, 0f);
                    shaft.transform.localScale = new Vector3(diameter * 0.38f, shaftHeight * 0.5f, diameter * 0.38f);
                    shaft.GetComponent<MeshRenderer>().sharedMaterial = material;
                    AddMesh(group, "Ponta", cone, new Vector3(0f, -sz * 0.5f + headHeight * 0.5f, 0f), Quaternion.identity, new Vector3(diameter, headHeight, diameter), material);
                    break;
                }
                case "seta_horizontal":
                {
                    float length = sx;
                    float diameter = Mathf.Max(sy, 0.002f);
                    float headLength = Mathf.Min(length * 0.4f, diameter * 1.6f);
                    float shaftLength = Mathf.Max(length - headLength, 0.001f);
                    GameObject shaft = CreatePrimitive(PrimitiveType.Cylinder, "Haste");
                    shaft.transform.SetParent(group.transform, false);
                    shaft.transform.localPosition = new Vector3(-length * 0.5f + shaftLength * 0.5f, 0f, 0f);
                    shaft.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    shaft.transform.localScale = new Vector3(diameter * 0.38f, shaftLength * 0.5f, diameter * 0.38f);
                    shaft.GetComponent<MeshRenderer>().sharedMaterial = material;
                    // A ponta do cone aponta para -Y; girar 90° em Z faz ela apontar para +X.
                    AddMesh(group, "Ponta", cone, new Vector3(length * 0.5f - headLength * 0.5f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(diameter, headLength, diameter), material);
                    break;
                }
                case "triangulo":
                    AddMesh(group, "Triângulo", triangle, Vector3.zero, Quaternion.identity, new Vector3(sx, sz, sy), material);
                    break;
                case "cilindro":
                {
                    // O cilindro primitivo tem 2 de altura: a escala Y é metade da altura.
                    GameObject cylinder = CreatePrimitive(PrimitiveType.Cylinder, "Cilindro");
                    cylinder.transform.SetParent(group.transform, false);
                    cylinder.transform.localScale = new Vector3(sx, sz * 0.5f, sy);
                    cylinder.GetComponent<MeshRenderer>().sharedMaterial = material;
                    break;
                }
                default:
                    AddBox(group, "Caixa", Vector3.zero, new Vector3(sx, sz, sy), material);
                    break;
            }

            return group;
        }

        /// <summary>
        /// Rotação em torno do eixo vertical. Positivo gira +x em direção a +y do quadro da placa
        /// (sentido horário visto de cima), que coincide com o sentido do Unity.
        /// Para o triângulo, o canto do ângulo reto aponta para o lado do deslocamento em relação ao alvo.
        /// </summary>
        private static float ShapeYaw(OverlayElementSpec element)
        {
            if (element.shape != "triangulo" || (Mathf.Abs(element.offset_x_mm) < 1f && Mathf.Abs(element.offset_y_mm) < 1f))
                return element.rotation_deg_about_up;

            // Malha base: ângulo reto no canto inferior esquerdo (x-, y+ no quadro da placa).
            bool left = element.offset_x_mm < 0f;
            bool bottom = element.offset_y_mm > 0f;
            if (left && bottom) return 0f;
            if (left) return 90f;
            if (!bottom) return 180f;
            return 270f;
        }

        private static OverlayMotionMode? ParseMotion(string motion)
        {
            switch ((motion ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "insert": return OverlayMotionMode.InsertLoop;
                case "bob": return OverlayMotionMode.Bob;
                default: return null;
            }
        }

        private static AssemblyStep BuildStepAsset(MontagemPreviewSpec spec, StepPreviewSpec step, GameObject overlayPrefab, Vector3 markerWorld)
        {
            string path = $"{StepsFolder}/Etapa_{step.step_id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyStep>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AssemblyStep>();
                AssetDatabase.CreateAsset(asset, path);
            }

            Vector3 offset = overlayPrefab != null ? TargetWorld(spec, step) - markerWorld : Vector3.zero;
            asset.name = Path.GetFileNameWithoutExtension(path);
            var so = new SerializedObject(asset);
            so.FindProperty("stepId").stringValue = step.step_id;
            so.FindProperty("title").stringValue = step.title_pt;
            so.FindProperty("componentName").stringValue = step.component_pt;
            so.FindProperty("requiredPartId").stringValue = step.part_id ?? string.Empty;
            so.FindProperty("requiredModelId").stringValue = step.model_id ?? string.Empty;
            so.FindProperty("minPartQuantity").intValue = Mathf.Max(1, step.part_min_qty);
            so.FindProperty("maxPartQuantity").intValue = Mathf.Max(0, step.part_max_qty);
            so.FindProperty("instruction").stringValue = step.instruction_pt;
            so.FindProperty("commonErrorAlert").stringValue = step.common_error_alert_pt;
            so.FindProperty("referenceImageName").stringValue = step.no_tracking ? string.Empty : MarkerName;
            so.FindProperty("overlayPrefab").objectReferenceValue = overlayPrefab;
            so.FindProperty("overlayPositionOffset").vector3Value = offset;
            so.FindProperty("overlayRotationOffset").vector3Value = Vector3.zero;
            so.FindProperty("validationType").enumValueIndex = (int)StepValidationType.Checklist;
            SerializedProperty items = so.FindProperty("checklistItems");
            string[] checklist = step.checklist_pt ?? Array.Empty<string>();
            items.arraySize = checklist.Length;
            for (int i = 0; i < checklist.Length; i++)
                items.GetArrayElementAtIndex(i).stringValue = checklist[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void BuildProcedureAsset(List<AssemblyStep> steps)
        {
            string path = ProcedurePath;
            var procedure = AssetDatabase.LoadAssetAtPath<AssemblyProcedure>(path);
            if (procedure == null)
            {
                procedure = ScriptableObject.CreateInstance<AssemblyProcedure>();
                AssetDatabase.CreateAsset(procedure, path);
            }

            procedure.name = Path.GetFileNameWithoutExtension(path);
            var so = new SerializedObject(procedure);
            so.FindProperty("procedureId").stringValue = ProcedureId;
            so.FindProperty("title").stringValue = "Montagem em bancada v0: placa ATX genérica (posições aproximadas)";
            SerializedProperty list = so.FindProperty("steps");
            list.arraySize = steps.Count;
            for (int i = 0; i < steps.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(procedure);
        }

        private static void BuildScene(MontagemPreviewSpec spec, GameObject boardPrefab, List<AssemblyStep> steps, MaterialCache materials, Vector3 markerWorld)
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MatSpec mat = spec.mat;

            GameObject desk = CreatePrimitive(PrimitiveType.Cube, "Mesa");
            desk.transform.position = new Vector3(0f, -0.015f, 0f);
            desk.transform.localScale = new Vector3(Mathf.Max(1.4f, mat.width_mm / 1000f + 0.6f), 0.03f, Mathf.Max(1.0f, mat.height_mm / 1000f + 0.5f));
            desk.GetComponent<MeshRenderer>().sharedMaterial = materials.Lit(new Color(0.55f, 0.38f, 0.24f), false);

            GameObject matQuad = CreatePrimitive(PrimitiveType.Quad, "Tapete de montagem (impresso)");
            matQuad.transform.position = new Vector3(0f, MatTop, 0f);
            matQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            matQuad.transform.localScale = new Vector3(mat.width_mm / 1000f, mat.height_mm / 1000f, 1f);
            Texture2D matTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MatTexturePath);
            matQuad.GetComponent<MeshRenderer>().sharedMaterial = matTexture != null
                ? materials.Textured("Tapete", matTexture, 0.05f)
                : materials.Lit(new Color(0.91f, 0.93f, 0.94f), false);

            var board = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab);
            board.transform.position = MatToWorld(mat, mat.board_left_mm, mat.board_top_mm, 0f);

            var tracked = new GameObject(TrackedImageObjectName);
            tracked.transform.position = markerWorld;

            bool first = true;
            foreach (AssemblyStep step in steps)
            {
                if (step.OverlayPrefab == null)
                    continue;
                // Igual ao ARContentManager: filho da imagem rastreada, no offset da etapa.
                var overlay = (GameObject)PrefabUtility.InstantiatePrefab(step.OverlayPrefab, tracked.transform);
                overlay.transform.SetLocalPositionAndRotation(step.OverlayPositionOffset, step.OverlayRotationOffset);
                overlay.SetActive(first);
                first = false;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            lightObject.transform.rotation = Quaternion.Euler(58f, -32f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.56f, 0.56f, 0.6f);

            var cameraObject = new GameObject(CameraObjectName);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.02f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.16f, 0.18f);
            cameraObject.tag = "MainCamera";
            PlaceCamera(camera, spec, spec.shots[0]);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // ---------------------------------------------------------------- renderização

        /// <summary>Renderiza cada câmera do spec em PNG, com o overlay da etapa correspondente.</summary>
        public static string RenderShots(string outDir, int width, int height, float motionTime)
        {
            MontagemPreviewSpec spec = LoadSpec();
            Camera camera = OpenPreviewScene();
            Directory.CreateDirectory(outDir);
            var written = new List<string>();

            foreach (ShotSpec shot in spec.shots)
            {
                GameObject overlay = ShowStep(shot.step_id);
                ApplyMotion(overlay, motionTime);
                PlaceCamera(camera, spec, shot);
                string file = Path.Combine(outDir, shot.name + ".png");
                RenderToPng(camera, file, width, height);
                ResetMotion(overlay);
                written.Add(Path.GetFileName(file));
            }

            ShowStep(spec.shots[0].step_id);
            PlaceCamera(camera, spec, spec.shots[0]);
            return $"{written.Count} imagens em {outDir}: {string.Join(", ", written)}";
        }

        /// <summary>
        /// Renderiza quadros de uma animação: a câmera do celular com um leve balanço de mão
        /// e o overlay da etapa em movimento (mesma conta do OverlayMotion no celular).
        /// </summary>
        public static string RenderFrames(string shotName, string outDir, int frames, float seconds, int width, int height)
        {
            MontagemPreviewSpec spec = LoadSpec();
            Camera camera = OpenPreviewScene();
            ShotSpec shot = Array.Find(spec.shots, s => s.name == shotName);
            if (shot == null)
                throw new ArgumentException($"Câmera '{shotName}' não existe no spec.");

            Directory.CreateDirectory(outDir);
            GameObject overlay = ShowStep(shot.step_id);
            Vector3 basePosition = MatToWorld(spec.mat, shot.cam_x_mm, shot.cam_y_mm, shot.cam_z_mm);
            Vector3 lookAt = MatToWorld(spec.mat, shot.look_x_mm, shot.look_y_mm, shot.look_z_mm);

            for (int i = 0; i < frames; i++)
            {
                float t = seconds * i / frames;
                float u = (float)i / frames * 2f * Mathf.PI;
                Vector3 sway = new Vector3(Mathf.Sin(u) * 0.012f, Mathf.Sin(u * 2f) * 0.005f, Mathf.Cos(u) * 0.006f);
                camera.transform.position = basePosition + sway;
                camera.transform.LookAt(lookAt);
                camera.transform.Rotate(0f, 0f, Mathf.Sin(u * 3f) * 1.2f, Space.Self);
                ApplyMotion(overlay, t);
                RenderToPng(camera, Path.Combine(outDir, i.ToString("000") + ".png"), width, height);
            }

            ResetMotion(overlay);
            PlaceCamera(camera, spec, shot);
            return $"{frames} quadros de '{shotName}' em {outDir}";
        }

        /// <summary>Ativa só o overlay da etapa indicada e o devolve.</summary>
        public static GameObject ShowStep(string stepId)
        {
            GameObject tracked = GameObject.Find(TrackedImageObjectName);
            if (tracked == null)
                throw new InvalidOperationException("Cena da prévia sem a imagem rastreada simulada. Rode Reconstruir antes.");

            GameObject shown = null;
            foreach (Transform child in tracked.transform)
            {
                bool match = child.name == "Overlay_" + stepId;
                child.gameObject.SetActive(match);
                if (match)
                    shown = child.gameObject;
            }
            return shown;
        }

        private static Camera OpenPreviewScene()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject cameraObject = GameObject.Find(CameraObjectName);
            if (cameraObject == null)
                throw new InvalidOperationException("Cena da prévia sem a câmera do celular. Rode Reconstruir antes.");
            return cameraObject.GetComponent<Camera>();
        }

        private static void ApplyMotion(GameObject overlay, float time)
        {
            if (overlay == null)
                return;
            foreach (OverlayMotion motion in overlay.GetComponentsInChildren<OverlayMotion>(true))
                motion.ApplyAt(time);
        }

        private static void ResetMotion(GameObject overlay)
        {
            if (overlay == null)
                return;
            foreach (OverlayMotion motion in overlay.GetComponentsInChildren<OverlayMotion>(true))
                motion.transform.localPosition = motion.RestLocalPosition;
        }

        private static void PlaceCamera(Camera camera, MontagemPreviewSpec spec, ShotSpec shot)
        {
            camera.transform.position = MatToWorld(spec.mat, shot.cam_x_mm, shot.cam_y_mm, shot.cam_z_mm);
            camera.transform.LookAt(MatToWorld(spec.mat, shot.look_x_mm, shot.look_y_mm, shot.look_z_mm));
        }

        private static void RenderToPng(Camera camera, string file, int width, int height)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(file, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        // ---------------------------------------------------------------- coordenadas

        /// <summary>Quadro do tapete (mm; origem no canto superior esquerdo; y para baixo; z para cima) para o mundo (m).</summary>
        public static Vector3 MatToWorld(MatSpec mat, float x, float y, float z)
        {
            return new Vector3((x - mat.width_mm * 0.5f) / 1000f, MatTop + z / 1000f, (mat.height_mm * 0.5f - y) / 1000f);
        }

        /// <summary>Quadro da placa (mm) para a posição local relativa ao canto superior esquerdo da placa.</summary>
        public static Vector3 BoardToLocal(float x, float y, float z)
        {
            return new Vector3(x / 1000f, z / 1000f, -y / 1000f);
        }

        /// <summary>Deslocamento relativo ao alvo (x direita, y para baixo, z para cima; mm) para metros locais.</summary>
        private static Vector3 OffsetToLocal(float x, float y, float z)
        {
            return new Vector3(x / 1000f, z / 1000f, -y / 1000f);
        }

        public static Vector3 MarkerWorld(MontagemPreviewSpec spec)
        {
            // O ARCore estima a imagem no plano do papel; 0,4 mm acima do tapete evita z-fighting na prévia.
            return MatToWorld(spec.mat, spec.mat.marker_center_x_mm, spec.mat.marker_center_y_mm, 0.4f);
        }

        private static Vector3 TargetWorld(MontagemPreviewSpec spec, StepPreviewSpec step)
        {
            BoardElementSpec pcb = Array.Find(spec.board_elements, e => e.id == "pcb");
            float pcbThickness = pcb != null && pcb.height_mm > 0f ? pcb.height_mm : DefaultPcbThicknessMm;
            return MatToWorld(spec.mat, spec.mat.board_left_mm + step.target_x_mm, spec.mat.board_top_mm + step.target_y_mm, pcbThickness + step.target_z_mm);
        }

        // ---------------------------------------------------------------- utilitários

        private static Color ToColor(float[] values, float alpha)
        {
            if (values == null || values.Length < 3)
                return new Color(0.5f, 0.5f, 0.5f, alpha);

            bool bytes = values[0] > 1f || values[1] > 1f || values[2] > 1f;
            float scale = bytes ? 1f / 255f : 1f;
            float a = values.Length > 3 ? values[3] * (values[3] > 1f ? 1f / 255f : 1f) : alpha;
            return new Color(values[0] * scale, values[1] * scale, values[2] * scale, a);
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
            return go;
        }

        private static void AddBox(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = CreatePrimitive(PrimitiveType.Cube, name);
            box.transform.SetParent(parent.transform, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddMesh(GameObject parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static Mesh EnsureMesh(string path, Action<Mesh> build)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew)
                mesh = new Mesh();
            mesh.Clear();
            build(mesh);
            if (isNew)
                AssetDatabase.CreateAsset(mesh, path);
            else
                EditorUtility.SetDirty(mesh);
            return mesh;
        }

        /// <summary>Cone de altura 1 e diâmetro 1, centrado na origem, com a ponta para -Y.</summary>
        private static void BuildConeMesh(Mesh mesh)
        {
            const int segments = 32;
            var triangles = new List<Vector3[]>();
            var apex = new Vector3(0f, -0.5f, 0f);
            var capCenter = new Vector3(0f, 0.5f, 0f);
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * 2f * Mathf.PI / segments;
                float a1 = (i + 1) * 2f * Mathf.PI / segments;
                var p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0.5f, Mathf.Sin(a0) * 0.5f);
                var p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0.5f, Mathf.Sin(a1) * 0.5f);
                triangles.Add(new[] { apex, p0, p1 });
                triangles.Add(new[] { capCenter, p0, p1 });
            }
            FillFlatMesh(mesh, triangles, new Vector3(0f, 0.1f, 0f));
        }

        /// <summary>Prisma triangular de 1x1x1 com o ângulo reto no canto x-, z- (inferior esquerdo da placa).</summary>
        private static void BuildRightTrianglePrismMesh(Mesh mesh)
        {
            var a = new Vector3(-0.5f, 0f, -0.5f);
            var b = new Vector3(0.5f, 0f, -0.5f);
            var c = new Vector3(-0.5f, 0f, 0.5f);
            var up = new Vector3(0f, 0.5f, 0f);
            var triangles = new List<Vector3[]>
            {
                new[] { a + up, b + up, c + up },
                new[] { a - up, b - up, c - up },
            };
            Vector3[][] edges = { new[] { a, b }, new[] { b, c }, new[] { c, a } };
            foreach (Vector3[] edge in edges)
            {
                triangles.Add(new[] { edge[0] + up, edge[1] + up, edge[1] - up });
                triangles.Add(new[] { edge[0] + up, edge[1] - up, edge[0] - up });
            }
            FillFlatMesh(mesh, triangles, new Vector3(-1f / 6f, 0f, -1f / 6f));
        }

        /// <summary>Malha com normais "flat"; a ordem dos vértices é corrigida para a normal apontar para fora do sólido convexo.</summary>
        private static void FillFlatMesh(Mesh mesh, List<Vector3[]> triangles, Vector3 interiorPoint)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();
            foreach (Vector3[] t in triangles)
            {
                Vector3 v0 = t[0], v1 = t[1], v2 = t[2];
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                Vector3 centroid = (v0 + v1 + v2) / 3f;
                if (Vector3.Dot(normal, centroid - interiorPoint) < 0f)
                {
                    (v1, v2) = (v2, v1);
                    normal = -normal;
                }
                int start = vertices.Count;
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                indices.Add(start);
                indices.Add(start + 1);
                indices.Add(start + 2);
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
        }

        /// <summary>Materiais reaproveitados por cor: Lit para a bancada (placa, tapete, mesa) e Unlit para os overlays.</summary>
        public class MaterialCache
        {
            private readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

            public Material Lit(Color color, bool metallic)
            {
                string name = "Placa_" + Hex(color) + (metallic ? "_metal" : string.Empty);
                return Get($"{BoardMaterialsFolder}/{name}.mat", "Universal Render Pipeline/Lit", m =>
                {
                    m.SetColor("_BaseColor", color);
                    m.SetFloat("_Metallic", metallic ? 0.85f : 0f);
                    m.SetFloat("_Smoothness", metallic ? 0.55f : 0.3f);
                    SetSurface(m, false);
                });
            }

            public Material Textured(string name, Texture2D texture, float smoothness)
            {
                return Get($"{BoardMaterialsFolder}/{name}.mat", "Universal Render Pipeline/Lit", m =>
                {
                    m.SetColor("_BaseColor", Color.white);
                    m.SetTexture("_BaseMap", texture);
                    m.SetFloat("_Metallic", 0f);
                    m.SetFloat("_Smoothness", smoothness);
                    SetSurface(m, false);
                });
            }

            public Material Overlay(Color color)
            {
                return Get($"{OverlayMaterialsFolder}/Overlay_{Hex(color)}.mat", "Universal Render Pipeline/Unlit", m =>
                {
                    m.SetColor("_BaseColor", color);
                    SetSurface(m, color.a < 0.999f);
                });
            }

            private Material Get(string path, string shaderName, Action<Material> setup)
            {
                if (cache.TryGetValue(path, out Material cached))
                    return cached;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find(shaderName));
                    AssetDatabase.CreateAsset(material, path);
                }
                else if (material.shader == null || material.shader.name != shaderName)
                {
                    material.shader = Shader.Find(shaderName);
                }

                setup(material);
                EditorUtility.SetDirty(material);
                cache[path] = material;
                return material;
            }

            private static void SetSurface(Material material, bool transparent)
            {
                material.SetFloat("_Surface", transparent ? 1f : 0f);
                BaseShaderGUI.SetMaterialKeywords(material);
            }

            private static string Hex(Color color)
            {
                return ColorUtility.ToHtmlStringRGBA(color);
            }
        }
    }
}
