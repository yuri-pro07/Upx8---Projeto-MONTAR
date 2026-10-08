using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace MontAR
{
    /// <summary>
    /// Grava o CSV da sessão em persistentDataPath/sessions/&lt;session_id&gt;.csv
    /// (recuperar com adb pull). Faz flush periódico para não perder dados se o app fechar.
    /// </summary>
    public class SessionMetricsLogger : MonoBehaviour
    {
        [Tooltip("Intervalo do flush em disco, em segundos.")]
        [SerializeField] private float flushIntervalSeconds = 2f;
        [Tooltip("Intervalo da amostra periódica de FPS (evento fps_sample), em segundos. 0 desliga.")]
        [SerializeField] private float fpsSampleIntervalSeconds = 5f;

        private readonly FpsWindow fps = new FpsWindow();
        private SessionCsvLog log;
        private StepFlow flow;
        private float flushTimer;
        private float sampleTimer;

        public static string SessionsDirectory => Path.Combine(Application.persistentDataPath, "sessions");

        public string CurrentFilePath { get; private set; }

        public bool IsLogging => log != null;

        /// <summary>
        /// Começa a gravar os eventos do fluxo. Se o arquivo da sessão já existir
        /// (sessão retomada), as novas linhas são anexadas sem repetir o cabeçalho.
        /// </summary>
        public void BeginSession(StepFlow stepFlow, SessionInfo info)
        {
            if (stepFlow == null)
                throw new ArgumentNullException(nameof(stepFlow));

            EndSession();

            Directory.CreateDirectory(SessionsDirectory);
            CurrentFilePath = Path.Combine(SessionsDirectory, info.SessionId + ".csv");
            bool appending = File.Exists(CurrentFilePath);
            var stream = new StreamWriter(CurrentFilePath, true, new UTF8Encoding(false));

            log = new SessionCsvLog(stream, info, () => Time.realtimeSinceStartupAsDouble, () => DateTime.UtcNow, !appending);
            flow = stepFlow;
            flow.EventRaised += OnFlowEvent;
            flushTimer = 0f;
            sampleTimer = 0f;

            Debug.Log($"[MontAR] Gravando métricas em {CurrentFilePath}");
        }

        public void EndSession()
        {
            if (flow != null)
                flow.EventRaised -= OnFlowEvent;
            flow = null;

            log?.Dispose();
            log = null;
        }

        private void OnFlowEvent(FlowEvent flowEvent)
        {
            if (log == null)
                return;

            log.Write(flowEvent, fps.Current);
            if (flowEvent.Type == FlowEventType.SessionCompleted)
                log.Flush();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fps.Tick(dt);
            if (log == null)
                return;

            flushTimer += dt;
            if (flushTimer >= flushIntervalSeconds)
            {
                flushTimer = 0f;
                log.Flush();
            }

            if (fpsSampleIntervalSeconds > 0f && flow.IsRunning)
            {
                sampleTimer += dt;
                if (sampleTimer >= fpsSampleIntervalSeconds)
                {
                    sampleTimer = 0f;
                    log.WriteRow(flow.CurrentIndex, flow.CurrentStep.StepId, "fps_sample", fps.Current, string.Empty);
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                log?.Flush();
        }

        private void OnDestroy()
        {
            EndSession();
        }
    }
}
