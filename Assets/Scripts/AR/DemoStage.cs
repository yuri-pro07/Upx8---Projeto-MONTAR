using UnityEngine;

namespace MontAR
{
    /// <summary>
    /// Bancada virtual da demonstração 3D: tapete, placa-mãe genérica e o mesmo overlay da etapa,
    /// preso a um marcador A virtual no mesmo offset usado na RA. Mostra "como faz" sem a câmera
    /// (aparelho sem ARCore, marcador fora de vista ou teste no Editor). A câmera orbita a peça
    /// da etapa; o gesto vem do <see cref="DemoOrbitInput"/>.
    /// </summary>
    public class DemoStage : MonoBehaviour
    {
        [SerializeField] private GameObject stageRoot;
        [Tooltip("Marcador A virtual: o overlay vira filho dele, como no ARTrackedImage.")]
        [SerializeField] private Transform markerAnchor;
        [Tooltip("Centro da placa virtual, enquadrado nas etapas sem overlay.")]
        [SerializeField] private Transform boardCenter;
        [SerializeField] private Camera stageCamera;
        [Tooltip("Fundo da câmera com a bancada à mostra.")]
        [SerializeField] private Color stageBackground = new Color(0.17f, 0.18f, 0.2f);
        [Tooltip("Fundo da câmera nos menus (bancada escondida), atrás das telas.")]
        [SerializeField] private Color menuBackground = new Color(0.957f, 0.957f, 0.961f);

        [Header("Câmera orbital")]
        [SerializeField] private float defaultYaw = 0f;
        [SerializeField] private float defaultPitch = 52f;
        [SerializeField] private float minPitch = 12f;
        [SerializeField] private float maxPitch = 88f;
        [SerializeField] private float minDistance = 0.12f;
        [SerializeField] private float maxDistance = 1.4f;
        [Tooltip("Raio da placa inteira (m), enquadrado nas etapas sem overlay.")]
        [SerializeField] private float boardRadius = 0.2f;
        [Tooltip("Folga do enquadramento em volta do overlay da etapa.")]
        [SerializeField] private float framingMargin = 1.15f;
        [Tooltip("Desloca o centro da imagem para cima (fração da meia-altura da tela), para a peça ficar na área livre entre o topo e o painel da etapa. A tela da montagem atualiza com SetFreeArea.")]
        [Range(-0.9f, 0.9f)]
        [SerializeField] private float verticalCenterOffset = 0.12f;
        [Tooltip("Fração da altura da tela livre para a peça (o resto fica sob o topo e o painel da etapa).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float visibleHeightFraction = 0.42f;
        [Tooltip("Graus de giro para um arrasto da altura inteira da tela.")]
        [SerializeField] private float degreesPerScreen = 220f;
        [SerializeField] private float smoothing = 10f;

        private GameObject overlay;
        private float yaw;
        private float pitch;
        private float distance;
        private Vector3 focus;
        private float targetYaw;
        private float targetPitch;
        private float targetDistance;
        private Vector3 targetFocus;
        private float centerOffset;
        private bool snap = true;
        private bool userAdjusted;

        public bool IsVisible => stageRoot != null && stageRoot.activeSelf;

        public Camera StageCamera => stageCamera;

        /// <summary>Etapa mostrada na bancada virtual (null depois de <see cref="Clear"/>).</summary>
        public AssemblyStep CurrentStep { get; private set; }

        public void SetVisible(bool visible)
        {
            if (stageRoot == null)
                return;
            if (visible && !stageRoot.activeSelf)
                snap = true;
            stageRoot.SetActive(visible);
            if (stageCamera != null)
                stageCamera.backgroundColor = visible ? stageBackground : menuBackground;
        }

        /// <summary>Mostra o overlay da etapa sobre a placa virtual e enquadra a peça.</summary>
        public void ShowStep(AssemblyStep step)
        {
            DestroyOverlay();
            CurrentStep = step;
            if (step != null && step.OverlayPrefab != null && markerAnchor != null)
            {
                overlay = Instantiate(step.OverlayPrefab, markerAnchor);
                overlay.transform.SetLocalPositionAndRotation(step.OverlayPositionOffset, step.OverlayRotationOffset);
            }
            ResetView();
        }

        public void Clear()
        {
            DestroyOverlay();
            CurrentStep = null;
            ResetView();
        }

        /// <summary>
        /// Volta para o ângulo padrão, enquadrando a peça da etapa (ou a placa inteira).
        /// <paramref name="immediate"/> pula a transição suave (ex.: capturas de tela).
        /// </summary>
        public void ResetView(bool immediate = false)
        {
            if (immediate)
                snap = true;
            userAdjusted = false;
            targetYaw = defaultYaw;
            targetPitch = defaultPitch;
            Frame();
        }

        /// <summary>
        /// Área da tela que sobra para a peça, em fração da altura (0 = base, 1 = topo): entre o painel
        /// da etapa e os cartões do topo. A peça fica centrada nessa faixa e cabe nela.
        /// </summary>
        public void SetFreeArea(float bottom, float top)
        {
            bottom = Mathf.Clamp01(bottom);
            top = Mathf.Clamp(top, Mathf.Min(1f, bottom + 0.1f), 1f);
            float offset = bottom + top - 1f;
            float fraction = top - bottom;
            if (Mathf.Abs(offset - verticalCenterOffset) < 0.005f && Mathf.Abs(fraction - visibleHeightFraction) < 0.005f)
                return;

            verticalCenterOffset = offset;
            visibleHeightFraction = fraction;
            // Só reenquadra se o usuário não girou nem aproximou desde o último "centralizar".
            if (!userAdjusted)
                Frame();
        }

        /// <summary>Gira a câmera em volta da peça. Arrastar para o lado gira, para cima/baixo inclina.</summary>
        public void Orbit(Vector2 deltaPixels)
        {
            float scale = degreesPerScreen / Mathf.Max(1f, Screen.height);
            targetYaw += deltaPixels.x * scale;
            targetPitch = Mathf.Clamp(targetPitch - deltaPixels.y * scale, minPitch, maxPitch);
            userAdjusted = true;
        }

        /// <summary>Aproxima (fator maior que 1) ou afasta (menor que 1) a câmera.</summary>
        public void Zoom(float factor)
        {
            if (factor <= 0f)
                return;
            targetDistance = Mathf.Clamp(targetDistance / factor, minDistance, maxDistance);
            userAdjusted = true;
        }

        private void Frame()
        {
            if (overlay != null && TryGetBounds(overlay, out Bounds bounds))
            {
                targetFocus = bounds.center;
                targetDistance = Mathf.Clamp(FitDistance(bounds.extents.magnitude), minDistance, maxDistance);
            }
            else
            {
                targetFocus = boardCenter != null ? boardCenter.position : transform.position;
                targetDistance = Mathf.Clamp(FitDistance(boardRadius), minDistance, maxDistance);
            }
        }

        private void LateUpdate()
        {
            if (!IsVisible || stageCamera == null)
                return;

            if (snap)
            {
                yaw = targetYaw;
                pitch = targetPitch;
                distance = targetDistance;
                focus = targetFocus;
                centerOffset = verticalCenterOffset;
                snap = false;
            }
            else
            {
                float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
                yaw = Mathf.LerpAngle(yaw, targetYaw, t);
                pitch = Mathf.Lerp(pitch, targetPitch, t);
                distance = Mathf.Lerp(distance, targetDistance, t);
                focus = Vector3.Lerp(focus, targetFocus, t);
                centerOffset = Mathf.Lerp(centerOffset, verticalCenterOffset, t);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            stageCamera.transform.SetPositionAndRotation(focus + rotation * (Vector3.back * distance), rotation);

            // Projeção descentralizada: o ponto focado sobe na tela sem inclinar a câmera.
            stageCamera.ResetProjectionMatrix();
            if (!Mathf.Approximately(centerOffset, 0f))
            {
                Matrix4x4 projection = stageCamera.projectionMatrix;
                projection.m12 = -centerOffset;
                stageCamera.projectionMatrix = projection;
            }
        }

        private float FitDistance(float radius)
        {
            if (stageCamera == null)
                return radius * 3f;

            // Só parte da altura fica livre (topo e painel da etapa cobrem o resto). Em retrato o campo
            // horizontal também é estreito: enquadra pelo menor dos dois.
            float verticalHalf = Mathf.Atan(Mathf.Tan(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * visibleHeightFraction);
            float horizontalHalf = Camera.VerticalToHorizontalFieldOfView(stageCamera.fieldOfView, Mathf.Max(0.1f, stageCamera.aspect)) * 0.5f * Mathf.Deg2Rad;
            float half = Mathf.Min(verticalHalf, horizontalHalf);
            return radius * framingMargin / Mathf.Sin(half);
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        private void DestroyOverlay()
        {
            if (overlay != null)
                Destroy(overlay);
            overlay = null;
        }
    }
}
