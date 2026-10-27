using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MontAR
{
    /// <summary>
    /// Instancia o overlay da etapa atual (peça "fantasma", seta de orientação) como filho
    /// da imagem de referência rastreada, no offset definido na etapa.
    /// </summary>
    public class ARContentManager : MonoBehaviour
    {
        [SerializeField] private ImageTrackingController imageTracking;
        [Tooltip("Overlay usado quando a etapa não tem prefab próprio (ex.: seta da prova de conceito).")]
        [SerializeField] private GameObject fallbackOverlayPrefab;
        [Tooltip("Mantém o overlay visível quando o rastreamento cai para Limited (ex.: mãos cobrindo o marcador).")]
        [SerializeField] private bool keepOverlayWhenLimited = true;
        [Tooltip("Etapa exibida ao iniciar, para testar o rastreamento sem o fluxo completo (prova de conceito).")]
        [SerializeField] private AssemblyStep previewStep;

        [Header("Peças virtuais (montagem sem o kit)")]
        [Tooltip("Marcador em que a placa-mãe virtual e as peças encaixadas ficam presas.")]
        [SerializeField] private string assemblyReferenceName = "MontAR_Marcador_A";
        [SerializeField] private GameObject virtualBoardPrefab;
        [Tooltip("Posição da placa-mãe virtual em relação ao marcador (a mesma do tapete).")]
        [SerializeField] private Vector3 virtualBoardOffset;
        [SerializeField] private Material installedMaterial;

        private readonly List<AssemblyStep> installedSteps = new List<AssemblyStep>();
        private AssemblyStep currentStep;
        private GameObject currentOverlay;
        private VirtualAssembly assembly;
        private bool simulated;

        public AssemblyStep CurrentStep => currentStep;

        /// <summary>Troca o overlay para a etapa indicada. Aparece assim que a referência dela for rastreada.</summary>
        public void ShowStep(AssemblyStep step)
        {
            currentStep = step;
            DestroyOverlay();

            if (step != null && imageTracking.TryGetTracked(step.ReferenceImageName, out ARTrackedImage image))
                Attach(image);
            if (simulated && imageTracking.TryGetTracked(assemblyReferenceName, out ARTrackedImage marker))
                EnsureAssembly(marker);
        }

        public void Clear()
        {
            currentStep = null;
            DestroyOverlay();
        }

        /// <summary>
        /// Peças virtuais: a placa-mãe virtual aparece sobre o marcador e as peças encaixadas ficam nela.
        /// Desligado, a RA só mostra a peça fantasma sobre o computador real.
        /// </summary>
        public void SetSimulated(bool value)
        {
            simulated = value;
            if (!simulated)
            {
                DestroyAssembly();
                return;
            }
            if (imageTracking != null && imageTracking.TryGetTracked(assemblyReferenceName, out ARTrackedImage marker))
                EnsureAssembly(marker);
        }

        /// <summary>Deixa a peça da etapa encaixada na placa virtual (animada se o marcador estiver à vista).</summary>
        public void InstallStep(AssemblyStep step, bool animate)
        {
            if (step == null || installedSteps.Contains(step))
                return;
            installedSteps.Add(step);
            if (assembly != null)
                assembly.Install(step, animate);
            if (step == currentStep)
                VirtualAssembly.SetGhostVisible(currentOverlay, false);
        }

        public void ClearInstalled()
        {
            installedSteps.Clear();
            if (assembly != null)
                assembly.Clear();
        }

        private void OnEnable()
        {
            if (imageTracking == null)
            {
                Debug.LogError("[MontAR] ARContentManager sem ImageTrackingController configurado.", this);
                enabled = false;
                return;
            }
            imageTracking.ReferenceTracked += OnReferenceTracked;
            imageTracking.ReferenceLost += OnReferenceLost;
        }

        private void OnDisable()
        {
            if (imageTracking == null)
                return;
            imageTracking.ReferenceTracked -= OnReferenceTracked;
            imageTracking.ReferenceLost -= OnReferenceLost;
        }

        private void Start()
        {
            if (previewStep != null && currentStep == null)
                ShowStep(previewStep);
        }

        private void OnReferenceTracked(ARTrackedImage image)
        {
            if (simulated && image.referenceImage.name == assemblyReferenceName)
                EnsureAssembly(image);

            if (currentStep == null || image.referenceImage.name != currentStep.ReferenceImageName)
                return;

            if (currentOverlay != null && currentOverlay.transform.parent == image.transform)
            {
                currentOverlay.SetActive(true);
                return;
            }

            Attach(image);
        }

        private void OnReferenceLost(string referenceName)
        {
            if (currentStep == null || referenceName != currentStep.ReferenceImageName || currentOverlay == null)
                return;

            if (!keepOverlayWhenLimited)
                currentOverlay.SetActive(false);
        }

        private void Attach(ARTrackedImage image)
        {
            DestroyOverlay();

            GameObject prefab = currentStep.OverlayPrefab != null ? currentStep.OverlayPrefab : fallbackOverlayPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[MontAR] A etapa '{currentStep.StepId}' não tem overlay nem prefab reserva.", this);
                return;
            }

            currentOverlay = Instantiate(prefab, image.transform);
            currentOverlay.transform.SetLocalPositionAndRotation(currentStep.OverlayPositionOffset, currentStep.OverlayRotationOffset);
            if (installedSteps.Contains(currentStep))
                VirtualAssembly.SetGhostVisible(currentOverlay, false);
            Debug.Log($"[MontAR] Overlay da etapa '{currentStep.StepId}' posicionado sobre '{image.referenceImage.name}'.");
        }

        /// <summary>Cria (ou move) a placa virtual e as peças encaixadas para o marcador rastreado.</summary>
        private void EnsureAssembly(ARTrackedImage image)
        {
            if (assembly != null && assembly.transform.parent == image.transform)
                return;

            DestroyAssembly();
            var root = new GameObject("Montagem virtual");
            root.transform.SetParent(image.transform, false);
            assembly = root.AddComponent<VirtualAssembly>();
            assembly.Configure(installedMaterial, virtualBoardPrefab, virtualBoardOffset);
            assembly.SetBoardVisible(true);
            foreach (AssemblyStep step in installedSteps)
                assembly.Install(step, false);
        }

        private void DestroyAssembly()
        {
            if (assembly != null)
                Destroy(assembly.gameObject);
            assembly = null;
        }

        private void DestroyOverlay()
        {
            if (currentOverlay != null)
                Destroy(currentOverlay);
            currentOverlay = null;
        }
    }
}
