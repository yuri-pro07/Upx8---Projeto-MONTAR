using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>Pino numerado preso a uma peça do PC. O número segue a ordem da montagem.</summary>
    public class StudyPinView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text numberLabel;
        [SerializeField] private RectTransform halo;

        private float phase;

        public Button Button => button;

        public void Bind(int number)
        {
            numberLabel.text = number.ToString();
            phase = number * 0.7f;
        }

        private void Update()
        {
            halo.localScale = Vector3.one * (1.05f + (Mathf.Sin(Time.unscaledTime * 2.6f + phase) + 1f) * 0.18f);
        }
    }
}
