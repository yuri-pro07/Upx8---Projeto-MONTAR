using UnityEngine;

namespace MontAR
{
    /// <summary>Tipo de movimento de um elemento do overlay.</summary>
    public enum OverlayMotionMode
    {
        /// <summary>Oscila entre a posição de repouso e o deslocamento (ex.: seta "apontando").</summary>
        Bob,

        /// <summary>Parte do deslocamento e desce até o encaixe, onde fica parado um tempo (ex.: CPU fantasma).</summary>
        InsertLoop
    }

    /// <summary>
    /// Conta do movimento dos overlays, separada do MonoBehaviour para ser testada em EditMode.
    /// </summary>
    public static class OverlayMotionMath
    {
        /// <summary>Deslocamento em relação à posição de repouso no instante indicado.</summary>
        public static Vector3 Offset(OverlayMotionMode mode, Vector3 travel, float periodSeconds, float holdFraction, float time)
        {
            if (periodSeconds <= 0f)
                return Vector3.zero;

            float phase = Mathf.Repeat(time / periodSeconds, 1f);

            if (mode == OverlayMotionMode.Bob)
                return travel * (0.5f - 0.5f * Mathf.Cos(phase * 2f * Mathf.PI));

            float hold = Mathf.Clamp(holdFraction, 0f, 0.9f);
            float moving = 1f - hold;
            if (phase >= moving)
                return Vector3.zero;

            float progress = Mathf.SmoothStep(0f, 1f, phase / moving);
            return travel * (1f - progress);
        }
    }

    /// <summary>
    /// Anima um elemento do overlay para mostrar o gesto da etapa: a peça fantasma descendo até
    /// o encaixe ou a seta oscilando. A posição de repouso fica serializada para que o mesmo
    /// movimento possa ser reproduzido no Editor (prévias renderizadas) e no celular.
    /// </summary>
    public class OverlayMotion : MonoBehaviour
    {
        [SerializeField] private OverlayMotionMode mode = OverlayMotionMode.Bob;
        [Tooltip("Deslocamento local em metros: ponto de partida (InsertLoop) ou amplitude (Bob).")]
        [SerializeField] private Vector3 travel = new Vector3(0f, 0.01f, 0f);
        [SerializeField] private float periodSeconds = 1.6f;
        [Tooltip("Fração do período em que a peça fica parada no encaixe (só InsertLoop).")]
        [Range(0f, 0.9f)]
        [SerializeField] private float holdFraction = 0.35f;
        [Tooltip("Posição local de repouso (no encaixe). Preenchida por quem monta o prefab.")]
        [SerializeField] private Vector3 restLocalPosition;

        public Vector3 RestLocalPosition => restLocalPosition;

        /// <summary>Configura o movimento. Usado pelo construtor de prefabs no Editor.</summary>
        public void Configure(OverlayMotionMode motionMode, Vector3 localTravel, float period, float hold, Vector3 restPosition)
        {
            mode = motionMode;
            travel = localTravel;
            periodSeconds = period;
            holdFraction = hold;
            restLocalPosition = restPosition;
            transform.localPosition = restPosition;
        }

        /// <summary>Coloca o elemento na posição do instante indicado.</summary>
        public void ApplyAt(float time)
        {
            transform.localPosition = restLocalPosition + OverlayMotionMath.Offset(mode, travel, periodSeconds, holdFraction, time);
        }

        private void Update()
        {
            ApplyAt(Time.time);
        }
    }
}
