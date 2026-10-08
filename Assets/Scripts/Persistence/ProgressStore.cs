using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MontAR
{
    /// <summary>
    /// Salva e carrega o progresso da montagem em JSON. Guarda uma sessão por aparelho.
    /// </summary>
    public class ProgressStore
    {
        public const string FileName = "progress.json";

        private readonly string directory;

        public ProgressStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("Diretório inválido.", nameof(directory));
            this.directory = directory;
        }

        /// <summary>Loja no persistentDataPath do aparelho.</summary>
        public static ProgressStore CreateDefault() => new ProgressStore(Application.persistentDataPath);

        public string FilePath => Path.Combine(directory, FileName);

        public void Save(SessionProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            progress.updatedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, JsonUtility.ToJson(progress, true));
        }

        /// <summary>Falso se não há progresso salvo ou se o arquivo está corrompido.</summary>
        public bool TryLoad(out SessionProgress progress)
        {
            progress = null;
            if (!File.Exists(FilePath))
                return false;

            try
            {
                progress = JsonUtility.FromJson<SessionProgress>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MontAR] Progresso salvo ilegível, ignorando: {e.Message}");
                progress = null;
            }

            if (progress == null || string.IsNullOrEmpty(progress.sessionId))
            {
                progress = null;
                return false;
            }
            return true;
        }

        public void Clear()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
