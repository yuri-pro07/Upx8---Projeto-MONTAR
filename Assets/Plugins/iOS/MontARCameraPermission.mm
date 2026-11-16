// Permissão da câmera no iOS, direto pelo AVFoundation.
// O Application.RequestUserAuthorization da Unity só pergunta quando o app usa WebCamTexture
// (UNITY_USES_WEBCAM). O MontAR usa a câmera pelo ARKit, então aquele pedido respondia
// "negado" sem o iPhone perguntar nada.

#import <AVFoundation/AVFoundation.h>

typedef void (*MontARCameraCallback)(int granted);

extern "C"
{
    // 0 = ainda não perguntado, 1 = restrito, 2 = negado, 3 = autorizado (AVAuthorizationStatus).
    int MontAR_CameraAuthorizationStatus()
    {
        return (int)[AVCaptureDevice authorizationStatusForMediaType: AVMediaTypeVideo];
    }

    // Abre o pedido do sistema. A resposta volta na thread principal, onde a Unity roda no iOS.
    void MontAR_RequestCameraAccess(MontARCameraCallback callback)
    {
        [AVCaptureDevice requestAccessForMediaType: AVMediaTypeVideo completionHandler: ^(BOOL granted) {
            dispatch_async(dispatch_get_main_queue(), ^{
                if (callback != NULL)
                    callback(granted ? 1 : 0);
            });
        }];
    }
}
