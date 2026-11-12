using UnityEngine;

namespace MontAR
{
    /// <summary>O que o enquadramento precisa saber da câmera. Na RA, a câmera não é controlada pelo app.</summary>
    public readonly struct StudyCameraInfo
    {
        public StudyCameraInfo(Vector3 position, Quaternion rotation, float tanHalfFov, float aspect)
        {
            Position = position;
            Rotation = rotation;
            TanHalfFov = tanHalfFov;
            Aspect = aspect;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        /// <summary>Tangente de metade do campo de visão vertical.</summary>
        public float TanHalfFov { get; }

        public float Aspect { get; }
        public Vector3 Forward => Rotation * Vector3.forward;
        public Vector3 Up => Rotation * Vector3.up;
    }

    /// <summary>Posição, rotação e escala uniforme do PC.</summary>
    public readonly struct StudyPose
    {
        public StudyPose(Vector3 position, Quaternion rotation, float scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public float Scale { get; }
    }

    /// <summary>
    /// Contas do "zoom" do estudo. Na RA o app não pode mover a câmera do celular, então aproximar é trazer
    /// o PC até ela: a peça escolhida vai para o centro da área livre da tela, no tamanho que couber.
    /// </summary>
    public static class StudyFraming
    {
        /// <summary>Ponto a <paramref name="distance"/> da câmera, na altura <paramref name="viewportY"/> da tela (0 = base, 1 = topo).</summary>
        public static Vector3 ViewPoint(in StudyCameraInfo camera, float viewportY, float distance)
        {
            return camera.Position + camera.Forward * distance + camera.Up * (distance * camera.TanHalfFov * (2f * viewportY - 1f));
        }

        /// <summary>Escala em que uma esfera de <paramref name="radius"/> cabe na faixa livre da tela, sem passar da largura.</summary>
        public static float FitScale(in StudyCameraInfo camera, float bottom, float top, float distance, float radius, float margin)
        {
            float halfVertical = distance * camera.TanHalfFov * Mathf.Max(0.05f, top - bottom);
            float halfHorizontal = distance * camera.TanHalfFov * camera.Aspect;
            return Mathf.Min(halfVertical, halfHorizontal) / Mathf.Max(0.001f, radius * margin);
        }

        /// <summary>
        /// Pose do PC que deixa o ponto <paramref name="center"/> (local ao PC) no centro da área livre, com a
        /// direção <paramref name="viewDirection"/> (local ao PC) virada para a câmera. Giro e zoom vêm do usuário.
        /// </summary>
        public static StudyPose Focus(in StudyCameraInfo camera, float bottom, float top, float distance, Vector3 center, Vector3 viewDirection,
            float radius, float margin, float yaw, float pitch, float zoom)
        {
            float scale = FitScale(camera, bottom, top, distance, radius, margin) * zoom;
            Quaternion facing = Quaternion.Inverse(Quaternion.LookRotation(-viewDirection.normalized, Vector3.up));
            Quaternion rotation = camera.Rotation * Quaternion.Euler(pitch, yaw, 0f) * facing;
            Vector3 target = ViewPoint(camera, (bottom + top) * 0.5f, distance);
            return new StudyPose(target - rotation * (center * scale), rotation, scale);
        }

        /// <summary>Giro em torno do eixo vertical que aponta o lado +X do PC para a câmera, mais <paramref name="offset"/> graus.</summary>
        public static float FacingYaw(Vector3 anchor, Vector3 cameraPosition, float offset)
        {
            Vector3 toCamera = cameraPosition - anchor;
            return Mathf.Atan2(-toCamera.z, toCamera.x) * Mathf.Rad2Deg + offset;
        }

        /// <summary>
        /// Posição da câmera do modo 3D para <paramref name="focus"/> ficar no centro da área livre,
        /// com uma esfera de <paramref name="radius"/> cabendo nela.
        /// </summary>
        public static Vector3 StageCameraPosition(Quaternion rotation, float tanHalfFov, float aspect, Vector3 focus, float radius, float margin, float bottom, float top)
        {
            float fit = tanHalfFov * Mathf.Min(Mathf.Max(0.05f, top - bottom), aspect);
            float distance = radius * margin / Mathf.Max(0.001f, fit);
            float centerY = (bottom + top) * 0.5f;
            return focus - rotation * Vector3.forward * distance - rotation * Vector3.up * (distance * tanHalfFov * (2f * centerY - 1f));
        }
    }
}
