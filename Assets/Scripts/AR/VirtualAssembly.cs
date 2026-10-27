using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MontAR
{
    /// <summary>
    /// Montagem com peças virtuais, para usar o app sem o kit: a placa-mãe virtual (na RA, presa ao
    /// marcador) e as peças já encaixadas, uma por etapa. Encaixar anima a peça no mesmo caminho da
    /// peça fantasma até o lugar dela, e ela fica montada. Fica no marcador (real ou virtual), então
    /// usa as mesmas posições do overlay da etapa.
    /// </summary>
    public class VirtualAssembly : MonoBehaviour
    {
        // Acabamento só para dar leitura ao conjunto (o spec não descreve a aparência real das peças),
        // pelo prefixo do id do elemento.
        private static readonly (string prefix, Color color, bool metallic)[] Finishes =
        {
            ("cpu", new Color(0.74f, 0.75f, 0.78f), true),
            ("cooler", new Color(0.8f, 0.82f, 0.85f), true),
            ("ram", new Color(0.3f, 0.36f, 0.46f), true),
            ("gpu", new Color(0.17f, 0.18f, 0.2f), false),
            ("drive", new Color(0.6f, 0.62f, 0.66f), true),
            ("ssd", new Color(0.1f, 0.12f, 0.16f), false),
            ("plugue", new Color(0.07f, 0.07f, 0.08f), false),
        };
        private static readonly Color DefaultFinish = new Color(0.25f, 0.27f, 0.31f);
        private static readonly Dictionary<GameObject, bool> HasPieceCache = new Dictionary<GameObject, bool>();

        [Tooltip("Material Lit de base das peças encaixadas (a cor muda por peça).")]
        [SerializeField] private Material installedMaterial;
        [Tooltip("Placa-mãe virtual (só na RA; a bancada 3D já tem a dela).")]
        [SerializeField] private GameObject boardPrefab;
        [SerializeField] private Vector3 boardLocalPosition;
        [SerializeField] private float fitSeconds = 0.8f;

        private readonly Dictionary<AssemblyStep, GameObject> installed = new Dictionary<AssemblyStep, GameObject>();
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private GameObject board;

        public void Configure(Material material, GameObject virtualBoard, Vector3 boardPosition)
        {
            installedMaterial = material;
            boardPrefab = virtualBoard;
            boardLocalPosition = boardPosition;
        }

        /// <summary>A etapa tem peça para encaixar (elemento verde no overlay)?</summary>
        public static bool HasPiece(AssemblyStep step)
        {
            if (step == null || step.OverlayPrefab == null)
                return false;
            if (HasPieceCache.TryGetValue(step.OverlayPrefab, out bool cached))
                return cached;

            bool found = false;
            foreach (OverlayPart part in step.OverlayPrefab.GetComponentsInChildren<OverlayPart>(true))
                found |= part.Role == OverlayPartRole.Piece;
            HasPieceCache[step.OverlayPrefab] = found;
            return found;
        }

        /// <summary>
        /// Mostra ou esconde a peça fantasma de um overlay (e o que se move com ela, como setas e marcas
        /// de onde segurar). Os contornos de onde encaixar continuam.
        /// </summary>
        public static void SetGhostVisible(GameObject overlay, bool visible)
        {
            if (overlay == null)
                return;
            foreach (OverlayPart part in overlay.GetComponentsInChildren<OverlayPart>(true))
            {
                if (part.Role == OverlayPartRole.Piece || part.GetComponent<OverlayMotion>() != null)
                    part.gameObject.SetActive(visible);
            }
        }

        public void SetBoardVisible(bool visible)
        {
            if (visible && board == null && boardPrefab != null)
            {
                board = Instantiate(boardPrefab, transform);
                board.name = "Placa-mãe virtual";
                board.transform.localPosition = boardLocalPosition;
            }
            if (board != null)
                board.SetActive(visible);
        }

        public bool Has(AssemblyStep step) => step != null && installed.ContainsKey(step);

        /// <summary>Deixa a peça da etapa encaixada. <paramref name="animate"/>: desce até o lugar.</summary>
        public void Install(AssemblyStep step, bool animate)
        {
            if (step == null || step.OverlayPrefab == null || installed.ContainsKey(step))
                return;

            GameObject root = Instantiate(step.OverlayPrefab, transform);
            root.name = "Peça encaixada: " + step.StepId;
            root.transform.SetLocalPositionAndRotation(step.OverlayPositionOffset, step.OverlayRotationOffset);
            installed[step] = root;

            var moves = new List<(Transform part, Vector3 from, Vector3 to)>();
            foreach (OverlayPart part in root.GetComponentsInChildren<OverlayPart>(true))
            {
                if (part.Role != OverlayPartRole.Piece)
                {
                    part.gameObject.SetActive(false);
                    Destroy(part.gameObject);
                    continue;
                }

                OverlayMotion motion = part.GetComponent<OverlayMotion>();
                Vector3 rest = motion != null ? motion.RestLocalPosition : part.transform.localPosition;
                Vector3 travel = motion != null && motion.Mode == OverlayMotionMode.InsertLoop && motion.Travel != Vector3.zero
                    ? motion.Travel
                    : new Vector3(0f, 0.02f, 0f);
                if (motion != null)
                {
                    motion.enabled = false;
                    Destroy(motion);
                }
                Paint(part);
                part.transform.localPosition = rest;
                moves.Add((part.transform, rest + travel, rest));
            }

            if (animate && isActiveAndEnabled && moves.Count > 0)
                StartCoroutine(Fit(moves));
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (GameObject piece in installed.Values)
            {
                if (piece != null)
                    Destroy(piece);
            }
            installed.Clear();
        }

        private void OnDestroy()
        {
            foreach (Material material in materials.Values)
                Destroy(material);
            materials.Clear();
        }

        private IEnumerator Fit(List<(Transform part, Vector3 from, Vector3 to)> moves)
        {
            foreach ((Transform part, Vector3 from, _) in moves)
                part.localPosition = from;

            for (float t = 0f; t < fitSeconds; t += Time.deltaTime)
            {
                float eased = EaseOutBack(Mathf.Clamp01(t / fitSeconds));
                foreach ((Transform part, Vector3 from, Vector3 to) in moves)
                {
                    if (part != null)
                        part.localPosition = Vector3.LerpUnclamped(from, to, eased);
                }
                yield return null;
            }

            foreach ((Transform part, _, Vector3 to) in moves)
            {
                if (part != null)
                    part.localPosition = to;
            }
        }

        /// <summary>Chega com um leve "clique": passa um pouco do lugar e volta.</summary>
        private static float EaseOutBack(float x)
        {
            const float c1 = 1.2f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private void Paint(OverlayPart part)
        {
            Material material = MaterialFor(part.ElementId ?? string.Empty);
            foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private Material MaterialFor(string elementId)
        {
            Color color = DefaultFinish;
            bool metallic = false;
            foreach ((string prefix, Color finish, bool isMetal) in Finishes)
            {
                if (elementId.StartsWith(prefix))
                {
                    color = finish;
                    metallic = isMetal;
                    break;
                }
            }

            string key = ColorUtility.ToHtmlStringRGB(color) + (metallic ? "_metal" : string.Empty);
            if (materials.TryGetValue(key, out Material cached))
                return cached;

            Material material = installedMaterial != null ? new Material(installedMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Peça encaixada " + key;
            material.color = color;
            material.SetFloat("_Metallic", metallic ? 0.8f : 0.1f);
            material.SetFloat("_Smoothness", metallic ? 0.6f : 0.4f);
            materials[key] = material;
            return material;
        }
    }
}
