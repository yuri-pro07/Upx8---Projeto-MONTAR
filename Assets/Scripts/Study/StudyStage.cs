using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MontAR
{
    /// <summary>
    /// Palco do Estudo do PC: coloca o computador 3D na mesa (RA, sem marcador) ou numa mesa virtual (3D),
    /// aproxima a peça tocada, separa as peças e anima o encaixe. Não usa o fluxo da montagem nem grava CSV.
    /// </summary>
    public class StudyStage : MonoBehaviour
    {
        private const float VirtualTableY = -200f;
        private static readonly Vector3 VirtualAnchor = new Vector3(0f, VirtualTableY, 0f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [Header("Cena")]
        [SerializeField] private Camera arCamera;
        [SerializeField] private Camera stageCamera;
        [SerializeField] private Light mainLight;
        [Tooltip("Detecção de planos. Fica desligada e só liga durante o estudo.")]
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARRaycastManager raycastManager;

        [Header("Conteúdo")]
        [Tooltip("Material Lit com emissão, base das peças.")]
        [SerializeField] private Material opaqueMaterial;
        [Tooltip("Material Unlit transparente: sombra de contato e mira.")]
        [SerializeField] private Material transparentMaterial;
        [SerializeField] private GameObject boardPrefab;

        [Header("Enquadramento")]
        [Tooltip("Tamanho do PC na mesa, em relação ao real (1 = 46 cm de altura).")]
        [SerializeField] private float tableScale = 0.6f;
        [SerializeField] private float sideAngle = 28f;
        [SerializeField] private float focusDistance = 0.42f;
        [SerializeField] private float focusMargin = 1.2f;
        [SerializeField] private float stageRadius = 0.36f;
        [SerializeField] private float stagePitch = 15f;
        [Tooltip("Intensidade da luz durante o estudo. Sobre a câmera real, o PC precisa de mais luz para não parecer apagado.")]
        [SerializeField] private float lightIntensity = 1.6f;
        [SerializeField] private float smoothing = 6.5f;
        [SerializeField] private float degreesPerScreen = 240f;

        [Header("Animação")]
        [SerializeField] private float introStep = 0.15f;
        [SerializeField] private float dimmedBrightness = 0.3f;

        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private MaterialPropertyBlock propertyBlock;
        private StudyPcModel model;
        private Transform anchor;
        private GameObject virtualTable;
        private Transform reticle;
        private Transform contactShadow;
        private Vector3 reticlePosition;
        private Quaternion restingLight;
        private float restingIntensity;
        private bool active;
        private bool snap;
        private float baseYaw;
        private float userYaw;
        private float userScale = 1f;
        private float focusYaw;
        private float focusPitch;
        private float focusZoom = 1f;
        private float freeBottom = 0.3f;
        private float freeTop = 0.85f;
        private float currentScale = 1f;
        private Vector3 currentPosition;
        private Quaternion currentRotation = Quaternion.identity;
        private float introStart = -1f;
        private float fitStart = -1f;

        /// <summary>O estado mudou: mesa encontrada, PC colocado, peça escolhida.</summary>
        public event Action StateChanged;

        public IReadOnlyList<StudyPart> Parts => model != null ? model.Parts : (IReadOnlyList<StudyPart>)Array.Empty<StudyPart>();

        public StudyPart Selected { get; private set; }

        public bool UsesCamera { get; private set; }

        public bool IsPlaced { get; private set; }

        /// <summary>A mira achou uma superfície.</summary>
        public bool HasSurface { get; private set; }

        public bool IsExploded { get; private set; }

        /// <summary>Há quanto tempo a câmera procura a mesa, em segundos.</summary>
        public float SearchSeconds { get; private set; }

        /// <summary>A animação de entrada do PC ou do encaixe está rodando.</summary>
        public bool IsAnimating => introStart >= 0f || fitStart >= 0f;

        public Camera ViewCamera => UsesCamera ? arCamera : stageCamera;

        public void Begin(bool useCamera)
        {
            EnsureModel();
            active = true;
            if (mainLight != null)
            {
                restingLight = mainLight.transform.rotation;
                restingIntensity = mainLight.intensity;
                mainLight.intensity = lightIntensity;
            }
            ApplyMode(useCamera);
        }

        public void End()
        {
            if (!active)
                return;
            active = false;
            Selected = null;
            IsExploded = false;
            IsPlaced = false;
            SetSurfaceSearch(false);
            anchor.gameObject.SetActive(false);
            model.Root.gameObject.SetActive(false);
            reticle.gameObject.SetActive(false);
            if (mainLight != null)
            {
                mainLight.transform.rotation = restingLight;
                mainLight.intensity = restingIntensity;
            }
        }

        /// <summary>Troca entre a câmera (RA) e a mesa virtual (3D), recomeçando a colocação.</summary>
        public void SetCameraMode(bool useCamera)
        {
            if (active && useCamera != UsesCamera)
                ApplyMode(useCamera);
        }

        /// <summary>Faixa da tela sem cartões por cima (0 = base, 1 = topo).</summary>
        public void SetFreeArea(float bottom, float top)
        {
            freeBottom = bottom;
            freeTop = top;
        }

        /// <summary>Coloca o PC onde a mira está. Falso se ela ainda não achou a mesa.</summary>
        public bool TryPlace()
        {
            if (!active || IsPlaced || !HasSurface)
                return false;
            Place(reticlePosition);
            return true;
        }

        /// <summary>Tira o PC da mesa para escolher outro lugar.</summary>
        public void Reposition()
        {
            if (!active || !UsesCamera)
                return;
            ClearSelection();
            SetExploded(false);
            IsPlaced = false;
            HasSurface = false;
            SearchSeconds = 0f;
            anchor.gameObject.SetActive(false);
            model.Root.gameObject.SetActive(false);
            SetSurfaceSearch(true);
            StateChanged?.Invoke();
        }

        /// <summary>Peça sob um ponto da tela (em pixels), ou null.</summary>
        public StudyPart Pick(Vector2 screenPoint)
        {
            if (!IsPlaced || ViewCamera == null)
                return null;
            if (!Physics.Raycast(ViewCamera.ScreenPointToRay(screenPoint), out RaycastHit hit, 30f))
                return null;
            for (Transform t = hit.collider.transform; t != null; t = t.parent)
            {
                StudyPart part = model.Parts.Find(p => p.Root == t);
                if (part != null)
                    return part;
            }
            return null;
        }

        /// <summary>Aproxima a peça e toca a animação do encaixe.</summary>
        public void Select(StudyPart part)
        {
            if (part == null || part == Selected)
                return;
            Selected = part;
            focusYaw = 0f;
            focusPitch = 0f;
            focusZoom = 1f;
            fitStart = part.FitOffset != Vector3.zero ? Time.unscaledTime + 0.55f : -1f;
            StateChanged?.Invoke();
        }

        public void ClearSelection()
        {
            if (Selected == null)
                return;
            Selected = null;
            fitStart = -1f;
            StateChanged?.Invoke();
        }

        public void SetExploded(bool value)
        {
            if (IsExploded == value)
                return;
            IsExploded = value;
            StateChanged?.Invoke();
        }

        /// <summary>Repete a animação do encaixe da peça aberta.</summary>
        public void PlayFit()
        {
            if (Selected != null && Selected.FitOffset != Vector3.zero)
                fitStart = Time.unscaledTime;
        }

        /// <summary>Arrasto: gira o PC na mesa ou, com uma peça aberta, gira a peça.</summary>
        public void Orbit(Vector2 deltaPixels)
        {
            float scale = degreesPerScreen / Mathf.Max(1f, Screen.height);
            if (Selected != null)
            {
                focusYaw -= deltaPixels.x * scale;
                focusPitch = Mathf.Clamp(focusPitch + deltaPixels.y * scale, -75f, 75f);
            }
            else if (IsPlaced)
            {
                userYaw -= deltaPixels.x * scale;
            }
        }

        /// <summary>Pinça: aproxima ou afasta.</summary>
        public void Zoom(float factor)
        {
            if (factor <= 0f)
                return;
            if (Selected != null)
                focusZoom = Mathf.Clamp(focusZoom * factor, 0.55f, 3f);
            else if (IsPlaced)
                userScale = Mathf.Clamp(userScale * factor, 0.45f, 2.2f);
        }

        /// <summary>Posição do pino da peça na tela (0 a 1). Falso se estiver atrás da câmera.</summary>
        public bool TryGetPinViewport(StudyPart part, out Vector2 viewport)
        {
            viewport = default;
            if (ViewCamera == null || !part.Root.gameObject.activeInHierarchy)
                return false;
            Vector3 point = ViewCamera.WorldToViewportPoint(model.Root.TransformPoint(part.PinLocal + part.Offset));
            viewport = point;
            return point.z > 0.02f;
        }

        // ------------------------------------------------------------ colocação

        private void EnsureModel()
        {
            if (model != null)
                return;
            propertyBlock = new MaterialPropertyBlock();
            model = StudyPcModel.Build(transform, opaqueMaterial, boardPrefab);
            model.Root.gameObject.SetActive(false);

            anchor = new GameObject("Âncora na mesa").transform;
            anchor.SetParent(transform, false);
            anchor.gameObject.SetActive(false);

            var shadowMaterial = new Material(transparentMaterial) { name = "Sombra de contato" };
            shadowMaterial.SetTexture(BaseMapId, BuildShadowTexture());
            shadowMaterial.SetColor(BaseColorId, new Color(0f, 0f, 0f, 0.62f));
            // Malha própria: GameObject.CreatePrimitive pode falhar em build com remoção de código da engine.
            var shadowMesh = new Mesh
            {
                name = "Sombra de contato",
                vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            shadowMesh.RecalculateBounds();
            var shadow = new GameObject("Sombra de contato");
            shadow.transform.SetParent(anchor, false);
            shadow.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            shadow.AddComponent<MeshFilter>().sharedMesh = shadowMesh;
            shadow.AddComponent<MeshRenderer>().sharedMaterial = shadowMaterial;
            contactShadow = shadow.transform;

            var table = new StudyMeshSketch();
            Material wood = new Material(opaqueMaterial) { name = "Mesa virtual" };
            wood.SetColor(BaseColorId, UiTheme.Hex(0x3A2C22));
            wood.SetFloat("_Smoothness", 0.35f);
            table.Cylinder(wood, new Vector3(0f, -0.02f, 0f), Vector3.up, 0.75f, 0.04f, 64);
            virtualTable = new GameObject("Mesa virtual");
            virtualTable.transform.SetParent(anchor, false);
            table.Bake(virtualTable.transform, new List<Renderer>());

            var reticleColor = new Material(transparentMaterial) { name = "Mira" };
            reticleColor.SetColor(BaseColorId, new Color(UiTheme.Primary.r, UiTheme.Primary.g, UiTheme.Primary.b, 0.9f));
            var sketch = new StudyMeshSketch();
            sketch.Ring(reticleColor, Vector3.zero, Vector3.up, 0.1f, 0.088f, 0.001f, 64);
            sketch.Cylinder(reticleColor, Vector3.zero, Vector3.up, 0.012f, 0.001f, 24);
            reticle = new GameObject("Mira na mesa").transform;
            reticle.SetParent(transform, false);
            sketch.Bake(reticle, new List<Renderer>());
            reticle.gameObject.SetActive(false);
        }

        private void ApplyMode(bool useCamera)
        {
            UsesCamera = useCamera;
            Selected = null;
            IsExploded = false;
            IsPlaced = false;
            HasSurface = false;
            SearchSeconds = 0f;
            userYaw = 0f;
            userScale = 1f;
            anchor.gameObject.SetActive(false);
            model.Root.gameObject.SetActive(false);
            reticle.gameObject.SetActive(false);
            virtualTable.SetActive(!useCamera);
            SetSurfaceSearch(useCamera);

            if (!useCamera)
            {
                // A câmera do 3D é a mesma da bancada da montagem, que pode ter deixado a projeção deslocada.
                stageCamera.ResetProjectionMatrix();
                Place(VirtualAnchor);
                return;
            }
            StateChanged?.Invoke();
        }

        private void SetSurfaceSearch(bool on)
        {
            if (planeManager != null)
                planeManager.enabled = on;
            if (raycastManager != null)
                raycastManager.enabled = on;
        }

        private void Place(Vector3 position)
        {
            anchor.SetPositionAndRotation(position, Quaternion.identity);
            anchor.gameObject.SetActive(true);
            model.Root.gameObject.SetActive(true);
            reticle.gameObject.SetActive(false);
            SetSurfaceSearch(false);
            IsPlaced = true;

            Vector3 viewer = UsesCamera ? arCamera.transform.position : position + Vector3.forward;
            baseYaw = StudyFraming.FacingYaw(position, viewer, sideAngle);
            userYaw = 0f;
            userScale = 1f;
            snap = true;

            // Entrada: cada peça chega na ordem da montagem.
            introStart = Time.unscaledTime;
            foreach (StudyPart part in model.Parts)
            {
                part.Offset = part.ExplodeOffset + new Vector3(0f, 0.12f, 0f);
                part.Visibility = part.Id == "gabinete" ? 1f : 0f;
                part.IntroDelay = part.Id == "gabinete" ? 0f : (Array.IndexOf(StudyPcModel.PartIds, part.Id) + 1) * introStep;
            }
            StateChanged?.Invoke();
        }

        private void UpdateSurfaceSearch(float deltaTime)
        {
            SearchSeconds += deltaTime;
            bool found = raycastManager != null
                && raycastManager.Raycast(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), hits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated);
            if (found)
            {
                reticlePosition = hits[0].pose.position;
                reticle.position = reticlePosition + Vector3.up * 0.002f;
                reticle.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f);
            }
            reticle.gameObject.SetActive(found);

            if (found != HasSurface)
            {
                HasSurface = found;
                StateChanged?.Invoke();
            }
        }

        // ------------------------------------------------------------ quadro a quadro

        private void LateUpdate()
        {
            if (!active)
                return;

            float deltaTime = Time.unscaledDeltaTime;
            if (UsesCamera)
                AimLight(arCamera.transform);

            if (!IsPlaced)
            {
                if (UsesCamera)
                    UpdateSurfaceSearch(deltaTime);
                return;
            }

            if (!UsesCamera)
                UpdateStageCamera();

            StudyCameraInfo view = CameraInfo(ViewCamera);
            float blend = snap ? 1f : 1f - Mathf.Exp(-smoothing * deltaTime);

            StudyPose target = Selected != null ? FocusPose(view) : OverviewPose();
            currentPosition = Vector3.Lerp(currentPosition, target.Position, blend);
            currentRotation = Quaternion.Slerp(currentRotation, target.Rotation, blend);
            currentScale = Mathf.Lerp(currentScale, target.Scale, blend);
            snap = false;

            model.Root.SetPositionAndRotation(currentPosition, currentRotation);
            model.Root.localScale = Vector3.one * currentScale;

            float tableSize = OverviewPose().Scale;
            contactShadow.localScale = new Vector3(0.62f * tableSize, 1f, 0.72f * tableSize);
            contactShadow.gameObject.SetActive(Selected == null);

            AnimateParts(deltaTime);
            model.Animate(Time.unscaledTime, deltaTime);
        }

        private StudyPose OverviewPose()
        {
            float scale = (UsesCamera ? tableScale : 1f) * userScale;
            return new StudyPose(anchor.position, Quaternion.Euler(0f, baseYaw + userYaw, 0f), scale);
        }

        private StudyPose FocusPose(in StudyCameraInfo view)
        {
            Bounds bounds = Selected.Bounds;
            return StudyFraming.Focus(view, freeBottom, freeTop, focusDistance, bounds.center + Selected.Offset, Selected.ViewDirection,
                bounds.extents.magnitude, focusMargin, focusYaw, focusPitch, focusZoom);
        }

        private void UpdateStageCamera()
        {
            Quaternion rotation = Quaternion.Euler(stagePitch, 180f, 0f);
            float tanHalf = Mathf.Tan(stageCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float radius = IsExploded ? stageRadius * 1.3f : stageRadius;
            Vector3 focus = anchor.position + Vector3.up * 0.2f;
            Vector3 position = StudyFraming.StageCameraPosition(rotation, tanHalf, Mathf.Max(0.1f, stageCamera.aspect), focus, radius, 1f, freeBottom, freeTop);
            float blend = snap ? 1f : 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            stageCamera.transform.SetPositionAndRotation(Vector3.Lerp(stageCamera.transform.position, position, blend), rotation);
            AimLight(stageCamera.transform);
        }

        /// <summary>Luz vinda de cima e de trás de quem olha: em qualquer ângulo, as peças ficam iluminadas.</summary>
        private void AimLight(Transform viewer)
        {
            if (mainLight != null)
                mainLight.transform.rotation = Quaternion.LookRotation(viewer.forward * 0.62f + Vector3.down * 0.72f - viewer.right * 0.32f);
        }

        private void AnimateParts(float deltaTime)
        {
            float now = Time.unscaledTime;
            float blend = 1f - Mathf.Exp(-9f * deltaTime);
            bool introDone = true;

            foreach (StudyPart part in model.Parts)
            {
                bool waiting = introStart >= 0f && now < introStart + part.IntroDelay;
                introDone &= !waiting;
                Vector3 targetOffset = waiting
                    ? part.ExplodeOffset + new Vector3(0f, 0.12f, 0f)
                    : IsExploded ? part.ExplodeOffset : Vector3.zero;
                part.Offset = Vector3.Lerp(part.Offset, targetOffset, blend);

                bool hidden = waiting || (Selected != null && Array.IndexOf(Selected.Occluders, part.Id) >= 0);
                part.Visibility = Mathf.MoveTowards(part.Visibility, hidden ? 0f : 1f, deltaTime * 4.5f);

                float eased = Mathf.SmoothStep(0f, 1f, part.Visibility);
                bool visible = eased > 0.01f;
                if (part.Root.gameObject.activeSelf != visible)
                    part.Root.gameObject.SetActive(visible);
                Vector3 fit = part == Selected && fitStart >= 0f ? part.FitOffset * FitCurve(now - fitStart) : Vector3.zero;
                part.Root.localScale = Vector3.one * eased;
                part.Root.localPosition = part.Offset + fit + part.Bounds.center * (1f - eased);

                float targetDim = Selected == null || part == Selected ? 1f : dimmedBrightness;
                if (!Mathf.Approximately(part.Dim, targetDim))
                {
                    part.Dim = Mathf.MoveTowards(part.Dim, targetDim, deltaTime * 3.5f);
                    StudyPcModel.ApplyDim(part, propertyBlock);
                }
            }

            if (introDone && introStart >= 0f && now > introStart + (model.Parts.Count + 2) * introStep)
                introStart = -1f;
            if (fitStart >= 0f && now - fitStart > FitSeconds)
                fitStart = -1f;
        }

        private const float FitSeconds = 1.5f;

        /// <summary>A peça sai do lugar, para um instante e volta encaixando.</summary>
        private static float FitCurve(float t)
        {
            if (t <= 0f || t >= FitSeconds)
                return 0f;
            if (t < 0.4f)
                return Mathf.SmoothStep(0f, 1f, t / 0.4f);
            if (t < 0.7f)
                return 1f;
            float back = (t - 0.7f) / (FitSeconds - 0.7f);
            return 1f - back * back * (3f - 2f * back);
        }

        private static StudyCameraInfo CameraInfo(Camera camera)
        {
            // A RA define a projeção direto na matriz: fieldOfView pode não acompanhar.
            float tanHalf = 1f / Mathf.Max(0.01f, Mathf.Abs(camera.projectionMatrix.m11));
            Transform t = camera.transform;
            return new StudyCameraInfo(t.position, t.rotation, tanHalf, Mathf.Max(0.1f, camera.aspect));
        }

        private static Texture2D BuildShadowTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Sombra", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                    pixels[x + y * size] = new Color32(255, 255, 255, (byte)(Mathf.SmoothStep(0f, 1f, alpha) * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }
    }
}
