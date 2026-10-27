using UnityEngine;

namespace MontAR
{
    /// <summary>Papel de um elemento do overlay, pela convenção de cores do spec da montagem.</summary>
    public enum OverlayPartRole
    {
        Other,
        /// <summary>Amarelo: onde encaixar (contornos, furos, triângulo do soquete).</summary>
        Target,
        /// <summary>Verde: a peça fantasma. Nas peças virtuais é ela que fica encaixada.</summary>
        Piece,
        /// <summary>Vermelho: chave, trava ou o que evitar.</summary>
        Lock,
        /// <summary>Laranja: seta do gesto.</summary>
        Gesture,
        /// <summary>Azul: onde segurar a peça.</summary>
        Grip
    }

    /// <summary>
    /// Marca o papel de um elemento do overlay. Posto pelo construtor dos overlays no Editor, a partir
    /// da cor do elemento no spec; as peças virtuais usam o papel para saber o que fica encaixado.
    /// </summary>
    public class OverlayPart : MonoBehaviour
    {
        [SerializeField] private string elementId;
        [SerializeField] private OverlayPartRole role;

        public string ElementId => elementId;

        public OverlayPartRole Role => role;

        public void Configure(string id, OverlayPartRole partRole)
        {
            elementId = id;
            role = partRole;
        }

        /// <summary>
        /// Convenção de cores do spec (valores de 0 a 1): amarelo = onde encaixar, verde = peça fantasma,
        /// vermelho = chave/trava ou o que evitar, laranja = gesto, azul = onde segurar.
        /// </summary>
        public static OverlayPartRole RoleFromColor(Color color)
        {
            if (color.g > 0.6f && color.r < 0.4f && color.b < 0.6f)
                return OverlayPartRole.Piece;
            if (color.r > 0.85f && color.g > 0.7f && color.b < 0.35f)
                return OverlayPartRole.Target;
            if (color.r > 0.85f && color.g < 0.3f)
                return OverlayPartRole.Lock;
            if (color.r > 0.85f && color.g >= 0.3f && color.g <= 0.7f)
                return OverlayPartRole.Gesture;
            if (color.b > 0.8f && color.r < 0.4f)
                return OverlayPartRole.Grip;
            return OverlayPartRole.Other;
        }
    }
}
