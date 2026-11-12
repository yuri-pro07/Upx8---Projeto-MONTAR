using System.Collections.Generic;
using UnityEngine;

namespace MontAR
{
    /// <summary>Uma peça do PC do estudo: geometria, como enquadrar, como encaixar e o estado da animação.</summary>
    public sealed class StudyPart
    {
        public StudyPart(string id, Transform root)
        {
            Id = id;
            Root = root;
        }

        public string Id { get; }
        public Transform Root { get; }

        /// <summary>Limites no espaço do PC, com a peça no lugar.</summary>
        public Bounds Bounds { get; internal set; }

        /// <summary>De que lado da peça a câmera olha ao aproximar (no espaço do PC).</summary>
        public Vector3 ViewDirection { get; internal set; } = Vector3.right;

        /// <summary>Por onde a peça chega ao encaixe. Zero: não há animação de encaixe.</summary>
        public Vector3 FitOffset { get; internal set; }

        public Vector3 ExplodeOffset { get; internal set; }

        /// <summary>Peças que saem de cena para esta aparecer (ex.: o cooler cobre a CPU).</summary>
        public string[] Occluders { get; internal set; } = new string[0];

        /// <summary>Onde o pino numerado fica, no espaço do PC.</summary>
        public Vector3 PinLocal { get; internal set; }

        public List<Renderer> Renderers { get; } = new List<Renderer>();

        /// <summary>1 com a peça em cena, 0 quando ela saiu.</summary>
        public float Visibility { get; internal set; } = 1f;

        internal Color[] BaseColors;
        internal Vector3 Offset;
        internal float Dim = 1f;
        internal float IntroDelay;
    }

    /// <summary>
    /// Monta, em tempo de execução, um PC aberto e detalhado em volta da mesma placa-mãe 3D usada na montagem.
    /// Espaço do PC, em metros: origem no chão, no centro do gabinete; +Y para cima; +Z para a frente;
    /// +X para o lado aberto, que fica virado para quem olha.
    /// </summary>
    public sealed class StudyPcModel
    {
        /// <summary>Peças criadas pelo modelo. Cada uma precisa de um texto em estudo_pc.json.</summary>
        public static readonly string[] PartIds =
        {
            "placa_mae", "cpu", "cooler", "ram", "ssd", "fonte", "cabos", "gpu", "ventoinhas", "gabinete"
        };

        private const float BoardX = -0.082f;
        private const float BoardTop = 0.44f;
        private const float BoardRear = -0.19f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

        private readonly Matrix4x4 boardFrame = Matrix4x4.TRS(new Vector3(BoardX, BoardTop, BoardRear), Quaternion.LookRotation(Vector3.up, Vector3.right), Vector3.one);
        private readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        private readonly List<(Transform rotor, float speed)> rotors = new List<(Transform, float)>();
        private readonly Material baseMaterial;
        private Material rgb;

        private StudyPcModel(Transform parent, Material opaque)
        {
            baseMaterial = opaque;
            Root = new GameObject("PC do estudo").transform;
            Root.SetParent(parent, false);
        }

        public Transform Root { get; }

        public List<StudyPart> Parts { get; } = new List<StudyPart>();

        public StudyPart Find(string id) => Parts.Find(part => part.Id == id);

        public static StudyPcModel Build(Transform parent, Material opaque, GameObject boardPrefab)
        {
            var model = new StudyPcModel(parent, opaque);
            model.rgb = model.Mat("RGB", Color.black, 0f, 0.4f);
            model.BuildCase();
            model.BuildBoard(boardPrefab);
            model.BuildCpu();
            model.BuildCooler();
            model.BuildRam();
            model.BuildSsd();
            model.BuildPsu();
            model.BuildCables();
            model.BuildGpu();
            model.BuildFans();
            foreach (StudyPart part in model.Parts)
                model.Capture(part);
            return model;
        }

        /// <summary>Gira as ventoinhas e muda a cor da iluminação RGB.</summary>
        public void Animate(float time, float deltaTime)
        {
            foreach ((Transform rotor, float speed) in rotors)
                rotor.Rotate(0f, 0f, speed * deltaTime, Space.Self);
            rgb.SetColor(EmissionId, Color.HSVToRGB(Mathf.Repeat(time * 0.08f, 1f), 0.8f, 1f) * 1.5f);
        }

        /// <summary>Escurece a peça sem mexer nos materiais compartilhados.</summary>
        public static void ApplyDim(StudyPart part, MaterialPropertyBlock block)
        {
            for (int i = 0; i < part.Renderers.Count; i++)
            {
                part.Renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, part.BaseColors[i] * new Color(part.Dim, part.Dim, part.Dim, 1f));
                part.Renderers[i].SetPropertyBlock(block);
            }
        }

        // ------------------------------------------------------------ peças

        private void BuildCase()
        {
            StudyPart part = NewPart("gabinete", new Vector3(0.85f, 0.25f, 0.5f), Vector3.zero, Vector3.zero);
            var s = new StudyMeshSketch();
            Material steel = Mat("Aço", Hex(0x1B1F27), 0.45f, 0.55f);
            Material inner = Mat("Aço interno", Hex(0x2A303B), 0.3f, 0.4f);
            Material plastic = Mat("Plástico", Hex(0x0E1014), 0f, 0.35f);
            Material alum = Mat("Alumínio", Hex(0xB4BAC2), 0.75f, 0.6f);

            AddCaseWall(part, s, inner, new Vector3(-0.1f, 0.23f, 0f), new Vector3(0.004f, 0.46f, 0.43f));
            AddCaseWall(part, s, steel, new Vector3(0f, 0.23f, -0.213f), new Vector3(0.21f, 0.46f, 0.004f));
            AddCaseWall(part, s, steel, new Vector3(0f, 0.23f, 0.213f), new Vector3(0.21f, 0.46f, 0.004f));
            AddCaseWall(part, s, steel, new Vector3(0f, 0.458f, 0f), new Vector3(0.21f, 0.004f, 0.43f));
            AddCaseWall(part, s, steel, new Vector3(0f, 0.014f, 0f), new Vector3(0.21f, 0.004f, 0.43f));

            // Moldura do lado aberto.
            s.Box(steel, new Vector3(0.103f, 0.449f, 0f), new Vector3(0.008f, 0.022f, 0.43f), 0.002f);
            s.Box(steel, new Vector3(0.103f, 0.025f, 0f), new Vector3(0.008f, 0.022f, 0.43f), 0.002f);
            s.Box(steel, new Vector3(0.103f, 0.237f, 0.206f), new Vector3(0.008f, 0.402f, 0.018f), 0.002f);
            s.Box(steel, new Vector3(0.103f, 0.237f, -0.206f), new Vector3(0.008f, 0.402f, 0.018f), 0.002f);

            // Frente: painel, grade e fita de LED.
            s.Box(plastic, new Vector3(0f, 0.237f, 0.223f), new Vector3(0.215f, 0.468f, 0.014f), 0.004f);
            for (int i = 0; i < 16; i++)
                s.Box(steel, new Vector3(0f, 0.05f + i * 0.0245f, 0.2305f), new Vector3(0.15f, 0.012f, 0.0012f));
            s.Box(rgb, new Vector3(0.09f, 0.24f, 0.2307f), new Vector3(0.0035f, 0.4f, 0.0016f));
            s.Box(rgb, new Vector3(-0.09f, 0.24f, 0.2307f), new Vector3(0.0035f, 0.4f, 0.0016f));

            // Topo: botão de ligar e portas.
            s.Cylinder(alum, new Vector3(0.05f, 0.4615f, 0.18f), Vector3.up, 0.009f, 0.004f);
            s.Ring(rgb, new Vector3(0.05f, 0.4625f, 0.18f), Vector3.up, 0.0115f, 0.0095f, 0.002f, 24);
            for (int i = 0; i < 2; i++)
                s.Box(plastic, new Vector3(0.012f - i * 0.022f, 0.4605f, 0.18f), new Vector3(0.015f, 0.002f, 0.007f));

            // Tampas dos slots e pés.
            for (int i = 0; i < 7; i++)
                s.Box(alum, new Vector3(0.018f, 0.16f + i * 0.0205f, -0.2158f), new Vector3(0.11f, 0.014f, 0.0012f));
            foreach (float x in new[] { -0.08f, 0.08f })
            {
                foreach (float z in new[] { -0.17f, 0.17f })
                    s.Cylinder(plastic, new Vector3(x, 0.006f, z), Vector3.up, 0.016f, 0.012f);
            }

            Finish(part, s, new Vector3(0.1f, 0.455f, 0.2f), false);
        }

        private void AddCaseWall(StudyPart part, StudyMeshSketch s, Material material, Vector3 center, Vector3 size)
        {
            s.Box(material, center, size);
            // Colisores por parede: um só, do tamanho do gabinete, engoliria os toques nas peças de dentro.
            var collider = part.Root.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        private void BuildBoard(GameObject boardPrefab)
        {
            StudyPart part = NewPart("placa_mae", new Vector3(1f, 0.12f, 0.3f), new Vector3(0.1f, 0f, 0f), Vector3.zero);
            GameObject board = Object.Instantiate(boardPrefab, part.Root);
            board.name = "Placa-mãe";
            board.transform.SetLocalPositionAndRotation(new Vector3(BoardX, BoardTop, BoardRear), Quaternion.LookRotation(Vector3.up, Vector3.right));
            foreach (Collider collider in board.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
            part.Renderers.AddRange(board.GetComponentsInChildren<Renderer>(true));

            var s = new StudyMeshSketch { Frame = boardFrame };
            Material dark = Mat("Dissipador", Hex(0x2A2F38), 0.55f, 0.5f);
            Material accent = Mat("Destaque", UiTheme.Primary, 0.1f, 0.5f, UiTheme.Primary * 0.5f);
            Material plastic = Mat("Plástico", Hex(0x0E1014), 0f, 0.35f);
            Material alum = Mat("Alumínio", Hex(0xB4BAC2), 0.75f, 0.6f);
            Material cap = Mat("Capacitor", Hex(0x23262D), 0.5f, 0.6f);

            s.Box(dark, B(86.5f, 19f, 28f), S(65f, 4f, 24f), 0.001f);
            s.Box(accent, B(86.5f, 19f, 30.2f), S(44f, 0.5f, 3f));
            s.Box(dark, B(41f, 77f, 28f), S(24f, 4f, 97f), 0.001f);
            s.Box(accent, B(41f, 77f, 30.2f), S(3f, 0.5f, 66f));
            s.Box(plastic, B(14f, 95f, 18f), S(27.5f, 33.5f, 162f), 0.0015f);
            s.Box(rgb, B(14f, 95f, 35f), S(2f, 0.6f, 120f));
            s.Box(dark, B(190f, 215f, 12.8f), S(46f, 3f, 46f), 0.001f);
            s.Box(accent, B(190f, 215f, 14.5f), S(20f, 0.5f, 3f));
            s.Box(alum, B(88f, 172f, 7.6f), S(91f, 11f, 9f), 0.0005f);
            for (int i = 0; i < 7; i++)
                s.Cylinder(cap, B(58f + i * 8.5f, 37f, 6f), Vector3.up, 0.003f, 0.009f, 12);
            for (int i = 0; i < 6; i++)
                s.Cylinder(cap, B(57f, 40f + i * 13f, 6f), Vector3.up, 0.003f, 0.009f, 12);

            s.Include(B(0f, 0f, 0f));
            s.Include(B(244f, 305f, 36f));
            Finish(part, s, boardFrame.MultiplyPoint3x4(B(92f, 288f, 6f)));
        }

        private void BuildCpu()
        {
            StudyPart part = NewPart("cpu", new Vector3(1f, 0.22f, 0.22f), new Vector3(0.055f, 0f, 0f), new Vector3(0.15f, 0.05f, 0.03f), "cooler");
            var s = new StudyMeshSketch { Frame = boardFrame };
            Material substrate = Mat("Substrato", Hex(0x0F4A2C), 0f, 0.45f);
            Material lid = Mat("Tampa da CPU", Hex(0xC8CDD4), 0.7f, 0.65f);
            Material gold = Mat("Triângulo", Hex(0xF5B301), 0f, 0.5f, Hex(0xF5B301) * 0.55f);
            Material mark = Mat("Gravação", Hex(0x5B6470), 0.2f, 0.4f);
            s.Box(substrate, B(108f, 78f, 5.2f), S(37.5f, 1.2f, 45f), 0.0003f);
            s.Box(lid, B(108f, 78f, 7.4f), S(30f, 3.2f, 37f), 0.0007f);
            s.Box(lid, B(108f, 78f, 6.2f), S(34f, 0.8f, 41f));
            // Triângulo do canto: é por ele que a CPU é alinhada ao soquete.
            s.Flat(gold, B(90.5f, 100f, 5.95f), B(97.5f, 100f, 5.95f), B(90.5f, 93f, 5.95f), Vector3.up);
            // Gravação a laser da tampa.
            for (int i = 0; i < 4; i++)
                s.Box(mark, B(108f, 66f + i * 5f, 9.05f), S(i == 0 ? 20f : 15f - i * 2f, 0.05f, 1.6f));
            Finish(part, s, boardFrame.MultiplyPoint3x4(B(128f, 120f, 12f)));
        }

        private void BuildCooler()
        {
            StudyPart part = NewPart("cooler", new Vector3(0.75f, 0.4f, -0.52f), new Vector3(0.09f, 0f, 0f), new Vector3(0.2f, 0.17f, -0.13f));
            var s = new StudyMeshSketch { Frame = boardFrame };
            Material alum = Mat("Aletas", Hex(0xC2C8CF), 0.6f, 0.55f);
            Material copper = Mat("Cobre", Hex(0xC07A3C), 0.75f, 0.6f);
            Material dark = Mat("Dissipador", Hex(0x2A2F38), 0.55f, 0.5f);
            Material accent = Mat("Destaque", UiTheme.Primary, 0.1f, 0.5f, UiTheme.Primary * 0.5f);
            Material frame = Mat("Moldura da ventoinha", Hex(0x171A20), 0f, 0.4f);
            Material blade = Mat("Pá", Hex(0x2C313A), 0f, 0.45f);

            s.Box(copper, B(108f, 78f, 11f), S(42f, 6f, 42f), 0.001f);
            s.Box(dark, B(108f, 78f, 16.5f), S(58f, 5f, 34f), 0.001f);
            for (int i = 0; i < 4; i++)
                s.Tube(copper, 0.0032f, B(93f + i * 10f, 78f, 13f), B(93f + i * 10f, 78f, 150f));

            for (int i = 0; i < 36; i++)
                s.Box(alum, B(108f, 78f, 50f + i * 2.8f), S(60f, 0.7f, 120f));
            s.Box(dark, B(108f, 78f, 153f), S(62f, 3.5f, 122f), 0.001f);
            s.Box(accent, B(108f, 78f, 155f), S(40f, 0.6f, 5f));

            s.Include(B(108f, 78f, 158f));
            AddFan(part, s, B(150.5f, 78f, 100f), Vector3.right, 0.12f, 0.025f, frame, blade, false, 520f);
            Finish(part, s, boardFrame.MultiplyPoint3x4(B(108f, 60f, 160f)));
        }

        private void BuildRam()
        {
            StudyPart part = NewPart("ram", new Vector3(1f, 0.32f, 0.38f), new Vector3(0.075f, 0f, 0f), new Vector3(0.2f, 0.13f, 0.16f));
            var s = new StudyMeshSketch { Frame = boardFrame };
            Material body = Mat("Dissipador da RAM", Hex(0x262B34), 0.55f, 0.55f);
            Material plate = Mat("Alumínio escovado", Hex(0x8E96A1), 0.7f, 0.55f);
            foreach (float x in new[] { 167.6f, 186.8f })
            {
                s.Box(body, B(x, 82f, 30f), S(6.5f, 38f, 133f), 0.001f);
                s.Box(plate, B(x - 3.4f, 82f, 28f), S(0.5f, 24f, 104f));
                s.Box(plate, B(x + 3.4f, 82f, 28f), S(0.5f, 24f, 104f));
                s.Box(rgb, B(x, 82f, 50.5f), S(5f, 3.2f, 127f), 0.0005f);
            }
            Finish(part, s, boardFrame.MultiplyPoint3x4(B(177f, 40f, 56f)));
        }

        private void BuildSsd()
        {
            StudyPart part = NewPart("ssd", new Vector3(1f, 0.42f, 0.12f), new Vector3(0.02f, 0f, -0.045f), new Vector3(0.17f, 0.04f, -0.2f), "cooler");
            var s = new StudyMeshSketch { Frame = boardFrame };
            Material pcb = Mat("PCB do SSD", Hex(0x0B2C1E), 0f, 0.5f);
            Material chip = Mat("Chip", Hex(0x101218), 0.2f, 0.6f);
            Material label = Mat("Etiqueta", Hex(0xE5E7EB), 0f, 0.35f);
            Material accent = Mat("Destaque", UiTheme.Primary, 0.1f, 0.5f, UiTheme.Primary * 0.5f);
            Material gold = Mat("Ouro", Hex(0xD4AF37), 0.8f, 0.6f);
            Material alum = Mat("Alumínio", Hex(0xB4BAC2), 0.75f, 0.6f);
            s.Box(pcb, B(81.5f, 140f, 5f), S(80f, 1f, 22f));
            s.Box(gold, B(119f, 140f, 5.6f), S(4f, 0.2f, 19f));
            s.Box(chip, B(106f, 140f, 6.2f), S(11f, 1.4f, 13f), 0.0003f);
            s.Box(chip, B(88f, 140f, 6.2f), S(15f, 1.4f, 15f), 0.0003f);
            s.Box(chip, B(66f, 140f, 6.2f), S(15f, 1.4f, 15f), 0.0003f);
            s.Box(label, B(68f, 140f, 7.1f), S(44f, 0.2f, 19f));
            s.Box(accent, B(68f, 134f, 7.3f), S(34f, 0.2f, 2.5f));
            s.Cylinder(alum, B(41f, 140f, 6.5f), Vector3.up, 0.0028f, 0.003f, 14);
            Finish(part, s, boardFrame.MultiplyPoint3x4(B(58f, 146f, 12f)));
        }

        private void BuildPsu()
        {
            StudyPart part = NewPart("fonte", new Vector3(1f, 0.3f, 0.3f), new Vector3(0.12f, 0f, 0f), new Vector3(0.2f, 0.02f, -0.14f));
            var s = new StudyMeshSketch();
            Material body = Mat("Corpo da fonte", Hex(0x1E222A), 0.4f, 0.5f);
            Material plastic = Mat("Plástico", Hex(0x0E1014), 0f, 0.35f);
            Material label = Mat("Etiqueta", Hex(0xE5E7EB), 0f, 0.35f);
            Material accent = Mat("Destaque", UiTheme.Primary, 0.1f, 0.5f, UiTheme.Primary * 0.5f);
            Material frame = Mat("Moldura da ventoinha", Hex(0x171A20), 0f, 0.4f);
            Material blade = Mat("Pá", Hex(0x2C313A), 0f, 0.45f);

            var center = new Vector3(0f, 0.064f, -0.125f);
            s.Box(body, center, new Vector3(0.15f, 0.086f, 0.16f), 0.003f);
            s.Box(label, center + new Vector3(0.0758f, 0f, 0.008f), new Vector3(0.0015f, 0.058f, 0.1f));
            s.Box(accent, center + new Vector3(0.0768f, 0.022f, 0.008f), new Vector3(0.0008f, 0.006f, 0.08f));
            s.Box(plastic, center + new Vector3(0.0768f, -0.006f, -0.015f), new Vector3(0.0008f, 0.02f, 0.042f));
            for (int i = 0; i < 4; i++)
                s.Box(plastic, center + new Vector3(0.0768f, -0.017f + i * 0.007f, 0.036f), new Vector3(0.0008f, 0.004f, 0.03f));

            // Conectores modulares, virados para dentro do gabinete.
            for (int i = 0; i < 4; i++)
                s.Box(plastic, center + new Vector3(-0.04f + i * 0.027f, -0.008f, 0.0815f), new Vector3(0.022f, 0.03f, 0.004f), 0.001f);

            AddFan(part, s, center + new Vector3(0f, -0.0445f, 0f), Vector3.down, 0.12f, 0.004f, frame, blade, false, 300f);
            Finish(part, s, center + new Vector3(0.078f, 0.012f, 0f));
        }

        private void BuildCables()
        {
            StudyPart part = NewPart("cabos", new Vector3(1f, 0.15f, 0.42f), new Vector3(0.06f, 0f, 0f), new Vector3(0.12f, 0f, 0.16f));
            var s = new StudyMeshSketch();
            Material sleeve = Mat("Malha do cabo", Hex(0x15171C), 0f, 0.45f);
            Material green = Mat("Malha verde", Hex(0x14823F), 0f, 0.45f);
            Material plastic = Mat("Plástico", Hex(0x0E1014), 0f, 0.35f);

            // 24 pinos: do conector, na borda direita da placa, até a fonte.
            var plug = new Vector3(-0.06f, 0.32f, 0.052f);
            s.Box(plastic, plug + new Vector3(-0.002f, 0f, 0.002f), new Vector3(0.018f, 0.054f, 0.013f), 0.001f);
            for (int i = 0; i < 4; i++)
            {
                float y = (i - 1.5f) * 0.012f;
                Material color = i % 2 == 0 ? sleeve : green;
                s.Tube(color, 0.0056f,
                    new Vector3(-0.048f, 0.32f + y, 0.053f),
                    new Vector3(-0.012f, 0.32f + y, 0.07f),
                    new Vector3(-0.012f, 0.16f + y * 0.6f, 0.076f),
                    new Vector3(-0.02f + y * 2f, 0.075f, 0.05f),
                    new Vector3(-0.02f + y * 2f, 0.056f, -0.044f));
            }

            // EPS: do canto superior esquerdo da placa para trás da chapa.
            var eps = new Vector3(-0.07f, 0.4325f, -0.15f);
            s.Box(plastic, eps + new Vector3(0.008f, 0f, 0f), new Vector3(0.014f, 0.011f, 0.021f), 0.001f);
            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1f) * 0.0065f;
                s.Tube(sleeve, 0.003f,
                    new Vector3(-0.058f, 0.4325f, -0.15f + z),
                    new Vector3(-0.035f, 0.447f, -0.15f + z),
                    new Vector3(-0.098f, 0.452f, -0.15f + z));
            }

            // PCIe 6+2: da fonte até a placa de vídeo.
            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1f) * 0.0065f;
                s.Tube(green, 0.003f,
                    new Vector3(0.05f, 0.141f + z * 0.2f, 0.06f + z),
                    new Vector3(0.05f, 0.14f, 0.088f + z),
                    new Vector3(0.012f, 0.08f, 0.088f + z),
                    new Vector3(0.018f, 0.056f, -0.044f + z));
            }

            Finish(part, s, new Vector3(-0.012f, 0.13f, 0.08f));
        }

        private void BuildGpu()
        {
            StudyPart part = NewPart("gpu", new Vector3(0.62f, -0.62f, 0.3f), new Vector3(0.1f, 0f, 0f), new Vector3(0.24f, -0.06f, 0.04f), "fonte", "cabos");
            var s = new StudyMeshSketch { Frame = boardFrame };
            Material shroud = Mat("Carcaça da GPU", Hex(0x20252E), 0.3f, 0.55f);
            Material pcb = Mat("PCB da GPU", Hex(0x10141A), 0f, 0.5f);
            Material plate = Mat("Backplate", Hex(0x3B424E), 0.65f, 0.55f);
            Material accent = Mat("Destaque", UiTheme.Primary, 0.1f, 0.5f, UiTheme.Primary * 0.5f);
            Material alum = Mat("Alumínio", Hex(0xB4BAC2), 0.75f, 0.6f);
            Material plastic = Mat("Plástico", Hex(0x0E1014), 0f, 0.35f);
            Material fins = Mat("Aletas", Hex(0xC2C8CF), 0.6f, 0.55f);
            Material frame = Mat("Moldura da ventoinha", Hex(0x171A20), 0f, 0.4f);
            Material blade = Mat("Pá", Hex(0x2C313A), 0f, 0.45f);

            s.Box(shroud, B(154f, 205f, 72f), S(292f, 118f, 50f), 0.004f);
            s.Box(pcb, B(154f, 177f, 72f), S(292f, 112f, 2f));
            s.Box(plate, B(154f, 174f, 72f), S(290f, 116f, 2f), 0.0007f);
            s.Box(accent, B(154f, 174f, 131.2f), S(240f, 0.6f, 2.5f));
            s.Box(rgb, B(154f, 205f, 131.3f), S(250f, 1f, 5f));
            s.Box(accent, B(60f, 194f, 131.3f), S(36f, 0.6f, 5f));

            // Aletas à mostra na borda, conector de energia e chapa de fixação.
            for (int i = 0; i < 44; i++)
                s.Box(fins, B(24f + i * 6f, 205f, 14f), S(1f, 3f, 40f));
            s.Box(plastic, B(262f, 190f, 134f), S(26f, 9f, 11f), 0.001f);
            s.Box(alum, B(-14f, 197f, 62f), S(1.4f, 122f, 40f));
            s.Box(plastic, B(-15f, 197f, 62f), S(0.6f, 80f, 12f));

            foreach (float x in new[] { 60f, 154f, 248f })
                AddFan(part, s, B(x, 230.6f, 72f), Vector3.back, 0.09f, 0.0015f, frame, blade, false, 420f);

            Finish(part, s, boardFrame.MultiplyPoint3x4(B(205f, 200f, 136f)));
        }

        private void BuildFans()
        {
            StudyPart part = NewPart("ventoinhas", new Vector3(0.68f, 0.18f, -0.7f), new Vector3(0f, 0f, -0.05f), new Vector3(0f, 0f, 0.1f));
            var s = new StudyMeshSketch();
            Material frame = Mat("Moldura da ventoinha", Hex(0x171A20), 0f, 0.4f);
            Material blade = Mat("Pá translúcida", Hex(0x3A414D), 0.1f, 0.6f);
            foreach (float y in new[] { 0.15f, 0.275f, 0.4f })
                AddFan(part, s, new Vector3(0f, y, 0.196f), Vector3.back, 0.12f, 0.025f, frame, blade, true, 360f);
            AddFan(part, s, new Vector3(0.018f, 0.372f, -0.198f), Vector3.back, 0.12f, 0.025f, frame, blade, true, 360f);
            Finish(part, s, new Vector3(0f, 0.275f, 0.18f));
        }

        // ------------------------------------------------------------ utilitários

        private void AddFan(StudyPart part, StudyMeshSketch s, Vector3 center, Vector3 axis, float size, float depth, Material frame, Material blade, bool lit, float speed)
        {
            Quaternion rotation = Quaternion.LookRotation(axis);
            float half = size * 0.5f;
            float bar = size * 0.09f;
            if (depth > 0.01f)
            {
                s.Box(frame, center + rotation * new Vector3(0f, half - bar * 0.5f, 0f), new Vector3(size, bar, depth), 0.001f, rotation);
                s.Box(frame, center + rotation * new Vector3(0f, -half + bar * 0.5f, 0f), new Vector3(size, bar, depth), 0.001f, rotation);
                s.Box(frame, center + rotation * new Vector3(half - bar * 0.5f, 0f, 0f), new Vector3(bar, size, depth), 0.001f, rotation);
                s.Box(frame, center + rotation * new Vector3(-half + bar * 0.5f, 0f, 0f), new Vector3(bar, size, depth), 0.001f, rotation);
            }
            s.Ring(frame, center, axis, half * 0.98f, half * 0.88f, Mathf.Max(depth, 0.003f));
            if (lit)
                s.Ring(rgb, center + axis.normalized * (depth * 0.5f + 0.0007f), axis, half * 0.93f, half * 0.875f, 0.0014f);

            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(part.Root, false);
            rotor.SetLocalPositionAndRotation(s.Frame.MultiplyPoint3x4(center), Quaternion.LookRotation(s.Frame.MultiplyVector(axis)));

            float hub = size * 0.2f;
            float tip = half * 0.86f;
            var sketch = new StudyMeshSketch();
            sketch.Cylinder(frame, Vector3.zero, Vector3.forward, hub, Mathf.Max(depth * 0.7f, 0.004f), 24);
            sketch.Blades(blade, hub * 0.9f, tip, (tip - hub) * 0.62f, 0.0012f, 9, 32f);
            sketch.Bake(rotor, part.Renderers);
            rotors.Add((rotor, speed));
        }

        private StudyPart NewPart(string id, Vector3 viewDirection, Vector3 fitOffset, Vector3 explodeOffset, params string[] occluders)
        {
            var root = new GameObject(id).transform;
            root.SetParent(Root, false);
            var part = new StudyPart(id, root)
            {
                ViewDirection = viewDirection.normalized,
                FitOffset = fitOffset,
                ExplodeOffset = explodeOffset,
                Occluders = occluders
            };
            Parts.Add(part);
            return part;
        }

        private static void Finish(StudyPart part, StudyMeshSketch s, Vector3 pin, bool boundsCollider = true)
        {
            s.Bake(part.Root, part.Renderers);
            part.Bounds = s.Bounds;
            part.PinLocal = pin;
            if (!boundsCollider)
                return;
            var collider = part.Root.gameObject.AddComponent<BoxCollider>();
            collider.center = s.Bounds.center;
            collider.size = s.Bounds.size;
        }

        /// <summary>Guarda a cor de cada renderer, que o escurecimento usa como base.</summary>
        private void Capture(StudyPart part)
        {
            part.BaseColors = new Color[part.Renderers.Count];
            for (int i = 0; i < part.Renderers.Count; i++)
            {
                Material material = part.Renderers[i].sharedMaterial;
                part.BaseColors[i] = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            }
        }

        private Material Mat(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            if (palette.TryGetValue(name, out Material material))
                return material;
            material = new Material(baseMaterial) { name = name };
            material.SetColor(BaseColorId, color);
            material.SetFloat(MetallicId, metallic);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetColor(EmissionId, emission ?? Color.black);
            palette[name] = material;
            return material;
        }

        private static Color Hex(int rgb) => UiTheme.Hex(rgb);

        /// <summary>Ponto no quadro da placa (mm): x a partir do painel traseiro, y a partir do topo, h acima da PCB.</summary>
        private static Vector3 B(float x, float y, float h) => new Vector3(x, h, -y) * 0.001f;

        /// <summary>Tamanho no quadro da placa (mm).</summary>
        private static Vector3 S(float x, float h, float y) => new Vector3(x, h, y) * 0.001f;
    }
}
