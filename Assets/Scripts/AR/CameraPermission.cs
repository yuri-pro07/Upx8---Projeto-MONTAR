using System;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
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
                return MontAR_CameraAuthorizationStatus() == IosAuthorized;
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
            // O iOS só pergunta uma vez: depois de negar (ou com a câmera restrita), só volta pelos Ajustes.
            if (MontAR_CameraAuthorizationStatus() != IosNotDetermined)
            {
                done(CameraPermissionResult.DeniedPermanently);
                return;
            }
            pendingIosAnswer = done;
            MontAR_RequestCameraAccess(IosAnswerCallback);
#else
            done(CameraPermissionResult.Granted);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        // Pedido pelo AVFoundation (Assets/Plugins/iOS/MontARCameraPermission.mm). O pedido da Unity
        // (Application.RequestUserAuthorization) não abre o diálogo sem WebCamTexture no projeto.
        private const int IosNotDetermined = 0;
        private const int IosAuthorized = 3;

        private delegate void IosCameraCallback(int granted);

        // Referência estática para o delegate não ser coletado enquanto o iOS não responde.
        private static readonly IosCameraCallback IosAnswerCallback = OnIosAnswer;
        private static Action<CameraPermissionResult> pendingIosAnswer;

        [DllImport("__Internal")]
        private static extern int MontAR_CameraAuthorizationStatus();

        [DllImport("__Internal")]
        private static extern void MontAR_RequestCameraAccess(IosCameraCallback callback);

        [AOT.MonoPInvokeCallback(typeof(IosCameraCallback))]
        private static void OnIosAnswer(int granted)
        {
            Action<CameraPermissionResult> done = pendingIosAnswer;
            pendingIosAnswer = null;
            done?.Invoke(granted != 0 ? CameraPermissionResult.Granted : CameraPermissionResult.DeniedPermanently);
        }
#endif
    }
}
