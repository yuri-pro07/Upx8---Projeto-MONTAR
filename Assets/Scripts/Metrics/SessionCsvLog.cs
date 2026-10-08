using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MontAR
{
    /// <summary>
    /// Escreve o log da sessão em CSV, formato longo (um evento por linha).
    /// Números sempre com ponto decimal (cultura invariante), mesmo em aparelho em pt-BR.
    /// </summary>
    public sealed class SessionCsvLog : IDisposable
    {
        public const string Header =
            "session_id,participant_code,app_version,device_model,step_index,step_id,event,t_ms,utc,fps_avg_1s,detail";

        private readonly TextWriter writer;
        private readonly SessionInfo info;
        private readonly Func<double> clockSeconds;
        private readonly Func<DateTime> utcNow;

        /// <param name="writer">Destino das linhas (arquivo em produção, StringWriter nos testes).</param>
        /// <param name="clockSeconds">Relógio monotônico em segundos (Time.realtimeSinceStartupAsDouble).</param>
        /// <param name="utcNow">Hora UTC, para cruzar com o vídeo da bancada e ordenar sessões retomadas.</param>
        /// <param name="writeHeader">Falso ao anexar a um arquivo que já tem cabeçalho.</param>
        public SessionCsvLog(TextWriter writer, SessionInfo info, Func<double> clockSeconds, Func<DateTime> utcNow, bool writeHeader)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.info = info;
            this.clockSeconds = clockSeconds ?? throw new ArgumentNullException(nameof(clockSeconds));
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));

            if (writeHeader)
                writer.WriteLine(Header);
        }

        public void Write(FlowEvent flowEvent, float fpsAverage)
        {
            WriteRow(flowEvent.StepIndex, flowEvent.StepId, flowEvent.Type.ToCsvName(), fpsAverage, flowEvent.Detail);
        }

        /// <summary>Linha avulsa, para eventos que não vêm do fluxo (ex.: amostra periódica de FPS).</summary>
        public void WriteRow(int stepIndex, string stepId, string eventName, float fpsAverage, string detail)
        {
            var line = new StringBuilder(160);
            line.Append(Escape(info.SessionId)).Append(',');
            line.Append(Escape(info.ParticipantCode)).Append(',');
            line.Append(Escape(info.AppVersion)).Append(',');
            line.Append(Escape(info.DeviceModel)).Append(',');
            line.Append(stepIndex >= 0 ? stepIndex.ToString(CultureInfo.InvariantCulture) : string.Empty).Append(',');
            line.Append(Escape(stepId)).Append(',');
            line.Append(Escape(eventName)).Append(',');
            line.Append(Math.Round(clockSeconds() * 1000.0).ToString("0", CultureInfo.InvariantCulture)).Append(',');
            line.Append(utcNow().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)).Append(',');
            line.Append(fpsAverage >= 0f && !float.IsNaN(fpsAverage) ? fpsAverage.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty).Append(',');
            line.Append(Escape(detail));
            writer.WriteLine(line.ToString());
        }

        public void Flush() => writer.Flush();

        public void Dispose() => writer.Dispose();

        /// <summary>Aspas no campo quando há vírgula, aspas ou quebra de linha (RFC 4180).</summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
