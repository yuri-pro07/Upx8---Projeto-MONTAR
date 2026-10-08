using UnityEngine;

namespace MontAR
{
    /// <summary>Cores da interface, compartilhadas pelas telas e pelo construtor da UI no Editor.</summary>
    public static class UiTheme
    {
        public static readonly Color Primary = Hex(0x16A34A);
        public static readonly Color PrimaryDark = Hex(0x15803D);
        public static readonly Color PrimarySoft = Hex(0xDCFCE7);
        public static readonly Color Accent = Hex(0xF97316);
        public static readonly Color Background = Hex(0xF4F4F5);
        public static readonly Color Surface = Color.white;
        public static readonly Color SurfaceMuted = Hex(0xF4F4F5);
        public static readonly Color SurfaceDark = new Color(0.09f, 0.09f, 0.1f, 0.9f);
        public static readonly Color Border = Hex(0xE4E4E7);
        public static readonly Color TextPrimary = Hex(0x18181B);
        public static readonly Color TextSecondary = Hex(0x52525B);
        public static readonly Color TextMuted = Hex(0x71717A);
        public static readonly Color TextOnDark = Hex(0xFAFAFA);
        public static readonly Color TextOnDarkMuted = Hex(0xA1A1AA);
        public static readonly Color AlertBackground = Hex(0xFFEDD5);
        public static readonly Color AlertText = Hex(0x9A3412);
        public static readonly Color Danger = Hex(0xDC2626);
        public static readonly Color Disabled = Hex(0xA1A1AA);
        public static readonly Color StatusLocating = Hex(0xD97706);
        public static readonly Color StatusGuiding = Hex(0x16A34A);
        public static readonly Color StatusDemo = Hex(0x2563EB);
        public static readonly Color StatusValidating = Hex(0x7C3AED);
        public static readonly Color Scrim = new Color(0f, 0f, 0f, 0.55f);

        public static Color Hex(int rgb, float alpha = 1f)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
        }
    }
}
