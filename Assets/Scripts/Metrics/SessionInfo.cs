namespace MontAR
{
    /// <summary>Dados fixos da sessão, repetidos em cada linha do CSV (formato longo).</summary>
    public readonly struct SessionInfo
    {
        public SessionInfo(string sessionId, string participantCode, string appVersion, string deviceModel)
        {
            SessionId = sessionId ?? string.Empty;
            ParticipantCode = participantCode ?? string.Empty;
            AppVersion = appVersion ?? string.Empty;
            DeviceModel = deviceModel ?? string.Empty;
        }

        public string SessionId { get; }

        /// <summary>Código anônimo do participante. Nunca um nome ou RA (LGPD).</summary>
        public string ParticipantCode { get; }

        public string AppVersion { get; }

        public string DeviceModel { get; }
    }
}
