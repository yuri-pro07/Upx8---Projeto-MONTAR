using UnityEngine;

namespace MontAR
{
    /// <summary>
    /// Cores da interface "vidro" (glassmorphism escuro): cartões de vidro fosco sobre o fundo aurora
    /// (menus e bancada 3D) ou sobre a câmera (RA), texto claro e o verde da marca como destaque.
    /// Compartilhadas pelas telas e pelo construtor da UI no Editor.
    /// </summary>
    public static class UiTheme
    {
        // Marca e destaques
        public static readonly Color Primary = Hex(0x22C55E);
        public static readonly Color PrimaryDark = Hex(0x16A34A);
        public static readonly Color PrimarySoft = Hex(0x22C55E, 0.18f);
        public static readonly Color PrimaryGlow = Hex(0x22C55E, 0.45f);
        public static readonly Color Accent = Hex(0xF97316);

        // Vidro: a cor que tinge o cartão (o shader mistura com o fundo desfocado) e os acabamentos.
        public static readonly Color GlassTint = Hex(0x0B1220);
        public static readonly Color GlassTintDeep = Hex(0x070B14);
        public static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.16f);
        public static readonly Color GlassBorderStrong = new Color(1f, 1f, 1f, 0.26f);
        public static readonly Color GlassHighlight = new Color(1f, 1f, 1f, 0.09f);
        public static readonly Color GlassFill = new Color(1f, 1f, 1f, 0.07f);
        public static readonly Color GlassFillStrong = new Color(1f, 1f, 1f, 0.13f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.35f);

        // Nomes antigos, agora no tom do vidro (ainda usados pelas telas).
        public static readonly Color Background = Hex(0x060A12);
        public static readonly Color Surface = GlassFill;
        public static readonly Color SurfaceMuted = GlassFill;
        public static readonly Color SurfaceDark = GlassTint;
        public static readonly Color Border = new Color(1f, 1f, 1f, 0.22f);

        // Texto
        public static readonly Color TextPrimary = Hex(0xF8FAFC);
        public static readonly Color TextSecondary = Hex(0xCBD5E1);
        public static readonly Color TextMuted = Hex(0x94A3B8);
        public static readonly Color TextOnDark = Hex(0xF8FAFC);
        public static readonly Color TextOnDarkMuted = Hex(0x94A3B8);
        public static readonly Color TextAccent = Hex(0x4ADE80);
        /// <summary>Texto sobre o verde do botão principal (verde quase preto: contraste alto).</summary>
        public static readonly Color TextOnPrimary = Hex(0x03160A);

        // Avisos e estados
        public static readonly Color AlertBackground = Hex(0xF59E0B, 0.16f);
        public static readonly Color AlertBorder = Hex(0xFBBF24, 0.38f);
        public static readonly Color AlertText = Hex(0xFDE68A);
        public static readonly Color Danger = Hex(0xEF4444, 0.26f);
        public static readonly Color DangerBorder = Hex(0xF87171, 0.55f);
        public static readonly Color DangerText = Hex(0xFECACA);
        public static readonly Color Disabled = Hex(0x64748B);
        public static readonly Color StatusLocating = Hex(0xF59E0B, 0.88f);
        public static readonly Color StatusGuiding = Hex(0x16A34A, 0.88f);
        public static readonly Color StatusDemo = Hex(0x2563EB, 0.88f);
        public static readonly Color StatusValidating = Hex(0x7C3AED, 0.88f);
        public static readonly Color Scrim = new Color(0.01f, 0.02f, 0.05f, 0.6f);

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }
    }
}
