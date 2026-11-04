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

        /// <summary>Negada sem o sistema perguntar de novo: só dá para liberar nas configurações do aparelho.</summary>
        DeniedPermanently
    }

    /// <summary>
    /// Permissão da câmera para a RA. No Android e no iOS abre o diálogo do sistema.
    /// No Editor e nas outras plataformas conta como concedida.
    /// </summary>
    public static class CameraPermission
    {
#if UNITY_IOS
        /// <summary>O que o sistema vai perguntar na próxima tela.</summary>
        public const string SystemPromptHint = "Na próxima tela o iPhone pergunta se o MontAR pode usar a câmera.";

        /// <summary>Onde liberar a câmera depois de negar.</summary>
        public const string SettingsHint = "Ajustes › MontAR › Câmera";
#else
        /// <summary>O que o sistema vai perguntar na próxima tela.</summary>
        public const string SystemPromptHint = "Na próxima tela o Android pergunta: <b>Durante o uso do app</b>, <b>Só desta vez</b> ou <b>Não permitir</b>.";

        /// <summary>Onde liberar a câmera depois de negar.</summary>
        public const string SettingsHint = "Configurações › Apps › MontAR › Permissões";
#endif

        public static bool IsGranted
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Camera);
#elif UNITY_IOS && !UNITY_EDITOR
                return Application.HasUserAuthorization(UserAuthorization.WebCam);
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
#elif UNITY_IOS && !UNITY_EDITOR
            // O iOS só pergunta uma vez: depois de negar, a câmera só volta pelos Ajustes.
            Application.RequestUserAuthorization(UserAuthorization.WebCam).completed += _ =>
                done(IsGranted ? CameraPermissionResult.Granted : CameraPermissionResult.DeniedPermanently);
#else
            done(CameraPermissionResult.Granted);
#endif
        }
    }
}
