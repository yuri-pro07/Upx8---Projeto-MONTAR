namespace MontAR
{
    /// <summary>
    /// Média de FPS na última janela completa de 1 segundo.
    /// Antes da primeira janela fechar, <see cref="Current"/> é negativo (sem medida).
    /// </summary>
    public class FpsWindow
    {
        private readonly float windowSeconds;
        private float elapsed;
        private int frames;

        public FpsWindow(float windowSeconds = 1f)
        {
            this.windowSeconds = windowSeconds;
            Current = -1f;
        }

        public float Current { get; private set; }

        /// <summary>Chamar uma vez por frame com o tempo real do frame (unscaled).</summary>
        public void Tick(float unscaledDeltaTime)
        {
            elapsed += unscaledDeltaTime;
            frames++;
            if (elapsed >= windowSeconds)
            {
                Current = frames / elapsed;
                elapsed = 0f;
                frames = 0;
            }
        }
    }
}
