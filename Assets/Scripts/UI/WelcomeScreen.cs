using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MontAR
{
    /// <summary>Uma página da apresentação do app.</summary>
    [Serializable]
    public class OnboardingPage
    {
        [SerializeField] private string title = "";
        [TextArea(3, 6)]
        [SerializeField] private string body = "";
        [SerializeField] private Sprite icon;

        public OnboardingPage()
        {
        }

        public OnboardingPage(string title, string body, Sprite icon)
        {
            this.title = title;
            this.body = body;
            this.icon = icon;
        }

        public string Title => title;
        public string Body => body;
        public Sprite Icon => icon;
    }

    /// <summary>
    /// Entrada do app: explica em poucas páginas como o MontAR funciona (Aponta, Guia, Valida,
    /// Registra). Dá para avançar pelos botões ou deslizando para o lado.
    /// </summary>
    public class WelcomeScreen : UiScreen, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private List<OnboardingPage> pages = new List<OnboardingPage>();
        [SerializeField] private Image pageIcon;
        [SerializeField] private TMP_Text pageTitle;
        [SerializeField] private TMP_Text pageBody;
        [SerializeField] private RectTransform dotsContainer;
        [SerializeField] private Image dotTemplate;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text versionLabel;
        [SerializeField] private float swipeThreshold = 90f;

        private readonly List<Image> dots = new List<Image>();
        private int page;

        /// <summary>O usuário terminou (ou pulou) a apresentação.</summary>
        public event Action Finished;

        public int Page => page;

        private void Awake()
        {
            nextButton.onClick.AddListener(Next);
            backButton.onClick.AddListener(() => Previous());
            skipButton.onClick.AddListener(() => Finished?.Invoke());
            if (versionLabel != null)
                versionLabel.text = $"versão {Application.version} · protótipo em desenvolvimento";
            BuildDots();
        }

        public override void Show()
        {
            base.Show();
            SetPage(0);
        }

        public void Next()
        {
            if (page >= pages.Count - 1)
                Finished?.Invoke();
            else
                SetPage(page + 1);
        }

        /// <summary>Volta uma página. Falso se já estava na primeira.</summary>
        public bool Previous()
        {
            if (page == 0)
                return false;
            SetPage(page - 1);
            return true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float dx = eventData.position.x - eventData.pressPosition.x;
            if (Mathf.Abs(dx) < swipeThreshold * transform.lossyScale.x)
                return;
            if (dx < 0f)
            {
                if (page < pages.Count - 1)
                    SetPage(page + 1);
            }
            else
            {
                Previous();
            }
        }

        private void SetPage(int index)
        {
            if (pages.Count == 0)
                return;

            page = Mathf.Clamp(index, 0, pages.Count - 1);
            OnboardingPage current = pages[page];
            pageTitle.text = current.Title;
            pageBody.text = current.Body;
            pageIcon.sprite = current.Icon;
            pageIcon.enabled = current.Icon != null;

            bool last = page == pages.Count - 1;
            nextLabel.text = last ? "Começar" : "Próximo";
            backButton.gameObject.SetActive(page > 0);
            skipButton.gameObject.SetActive(!last);

            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].color = i == page ? UiTheme.Primary : UiTheme.Border;
                dots[i].rectTransform.sizeDelta = new Vector2(i == page ? 44f : 18f, 18f);
            }
        }

        private void BuildDots()
        {
            dotTemplate.gameObject.SetActive(false);
            for (int i = 0; i < pages.Count; i++)
            {
                Image dot = Instantiate(dotTemplate, dotsContainer);
                dot.gameObject.SetActive(true);
                dot.name = $"Ponto {i + 1}";
                dots.Add(dot);
            }
        }
    }
}
