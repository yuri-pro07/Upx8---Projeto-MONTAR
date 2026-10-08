using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace MontAR
{
    /// <summary>
    /// Recebe os eventos do ARTrackedImageManager e os traduz em "referência rastreada" e
    /// "referência perdida". Só o estado Tracking conta como rastreada: Limited é tratado
    /// como perdido, porque a pose fica estimada e não serve para validar a etapa.
    /// </summary>
    public class ImageTrackingController : MonoBehaviour
    {
        [SerializeField] private ARTrackedImageManager trackedImageManager;

        private readonly Dictionary<TrackableId, string> trackedNames = new Dictionary<TrackableId, string>();
        private readonly Dictionary<string, ARTrackedImage> trackedImages = new Dictionary<string, ARTrackedImage>(StringComparer.Ordinal);
        private readonly HashSet<TrackableId> ignoredImages = new HashSet<TrackableId>();

        /// <summary>Uma referência passou para o estado Tracking.</summary>
        public event Action<ARTrackedImage> ReferenceTracked;

        /// <summary>Uma referência saiu do estado Tracking (Limited, None ou removida).</summary>
        public event Action<string> ReferenceLost;

        public IEnumerable<string> TrackedReferenceNames => trackedImages.Keys;

        public bool TryGetTracked(string referenceName, out ARTrackedImage image)
        {
            image = null;
            return !string.IsNullOrEmpty(referenceName)
                && trackedImages.TryGetValue(referenceName, out image)
                && image != null;
        }

        private void OnEnable()
        {
            if (trackedImageManager == null)
            {
                Debug.LogError("[MontAR] ImageTrackingController sem ARTrackedImageManager configurado.", this);
                enabled = false;
                return;
            }
            trackedImageManager.trackablesChanged.AddListener(OnTrackablesChanged);
        }

        private void OnDisable()
        {
            if (trackedImageManager != null)
                trackedImageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            foreach (ARTrackedImage image in args.added)
                UpdateState(image);

            foreach (ARTrackedImage image in args.updated)
                UpdateState(image);

            foreach (KeyValuePair<TrackableId, ARTrackedImage> removed in args.removed)
            {
                if (trackedNames.TryGetValue(removed.Key, out string referenceName))
                    MarkLost(removed.Key, referenceName);
            }
        }

        private void UpdateState(ARTrackedImage image)
        {
            bool isTracking = image.trackingState == TrackingState.Tracking;
            bool wasTracking = trackedNames.TryGetValue(image.trackableId, out string referenceName);

            if (isTracking && !wasTracking)
            {
                referenceName = image.referenceImage.name;
                if (string.IsNullOrEmpty(referenceName))
                {
                    // Imagem fora da XRReferenceImageLibrary (ex.: ambiente do XR Simulation).
                    if (ignoredImages.Add(image.trackableId))
                        Debug.LogWarning($"[MontAR] Imagem rastreada sem referência na biblioteca, ignorada (guid {image.referenceImage.guid}).");
                    return;
                }

                trackedNames[image.trackableId] = referenceName;
                trackedImages[referenceName] = image;
                Debug.Log($"[MontAR] Referência detectada: {referenceName}");
                ReferenceTracked?.Invoke(image);
            }
            else if (!isTracking && wasTracking)
            {
                MarkLost(image.trackableId, referenceName);
            }
        }

        private void MarkLost(TrackableId id, string referenceName)
        {
            trackedNames.Remove(id);
            trackedImages.Remove(referenceName);
            Debug.Log($"[MontAR] Referência perdida: {referenceName}");
            ReferenceLost?.Invoke(referenceName);
        }
    }
}
