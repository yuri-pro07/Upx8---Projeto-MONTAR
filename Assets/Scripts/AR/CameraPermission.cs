using System;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace MontAR
{
    /// <summary>Resposta do pedido de permissão da câmera.</summary>
    public enum CameraPermissionResult
    {
        Granted,
        Denied,

        /// <summary>Negada com "não perguntar de novo": só dá para liberar nas configurações do Android.</summary>
        DeniedPermanently
    }

    /// <summary>
    /// Permissão da câmera para a RA. No Android abre o diálogo do sistema ("Durante o uso do app",
    /// "Só desta vez", "Não permitir"). No Editor e nas outras plataformas conta como concedida.
    /// </summary>
    public static class CameraPermission
    {
        public static bool IsGranted
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
                return true;
#endif
            }
        }

        /// <summary>Pede a permissão. O callback roda na thread principal, uma única vez.</summary>
        public static void Request(Action<CameraPermissionResult> done)
        {
            if (done == null)
                throw new ArgumentNullException(nameof(done));
            if (IsGranted)
            {
                done(CameraPermissionResult.Granted);
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            bool answered = false;
            void Answer(CameraPermissionResult result)
            {
                if (answered)
                    return;
                answered = true;
                done(result);
            }

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => Answer(CameraPermissionResult.Granted);
            // Depois de "Não permitir" duas vezes, o Android para de perguntar e não mostra mais a justificativa.
            callbacks.PermissionDenied += _ => Answer(Permission.ShouldShowRequestPermissionRationale(Permission.Camera)
                ? CameraPermissionResult.Denied
                : CameraPermissionResult.DeniedPermanently);
            callbacks.PermissionRequestDismissed += _ => Answer(CameraPermissionResult.Denied);
            Permission.RequestUserPermission(Permission.Camera, callbacks);
#else
            done(CameraPermissionResult.Granted);
#endif
        }
    }
}
