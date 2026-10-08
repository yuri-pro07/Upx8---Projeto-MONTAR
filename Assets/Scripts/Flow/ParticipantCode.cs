using System;
using System.Globalization;

namespace MontAR
{
    /// <summary>
    /// Regras do código anônimo do participante. O código vira parte do nome do arquivo
    /// CSV, então só aceita letras sem acento, dígitos, hífen e sublinhado.
    /// </summary>
    public static class ParticipantCode
    {
        public const int MaxLength = 16;

        /// <summary>Remove espaços nas pontas e deixa em maiúsculas (ex.: " p01 " → "P01").</summary>
        public static string Normalize(string code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        public static bool IsValid(string code)
        {
            string normalized = Normalize(code);
            if (normalized.Length == 0 || normalized.Length > MaxLength)
                return false;

            foreach (char c in normalized)
            {
                bool allowed = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!allowed)
                    return false;
            }
            return true;
        }

        /// <summary>Id da sessão: código + hora UTC de início (ex.: "P01_20261020T143000").</summary>
        public static string CreateSessionId(string code, DateTime utcNow)
        {
            if (!IsValid(code))
                throw new ArgumentException("Código de participante inválido.", nameof(code));
            return Normalize(code) + "_" + utcNow.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);
        }
    }
}
