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

        private AssemblyStep currentStep;
        private GameObject currentOverlay;

        public AssemblyStep CurrentStep => currentStep;

        /// <summary>Troca o overlay para a etapa indicada. Aparece assim que a referência dela for rastreada.</summary>
        public void ShowStep(AssemblyStep step)
        {
            currentStep = step;
            DestroyOverlay();

            if (step != null && imageTracking.TryGetTracked(step.ReferenceImageName, out ARTrackedImage image))
                Attach(image);
        }

        public void Clear()
        {
            currentStep = null;
            DestroyOverlay();
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
            Debug.Log($"[MontAR] Overlay da etapa '{currentStep.StepId}' posicionado sobre '{image.referenceImage.name}'.");
        }

        private void DestroyOverlay()
        {
            if (currentOverlay != null)
                Destroy(currentOverlay);
            currentOverlay = null;
        }
    }
}
