using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MontAR.EditorTools
{
    /// <summary>Blocos de construção da interface (uGUI + TextMeshPro) usados pelo <see cref="AppBuilder"/>.</summary>
    public static class UiFactory
    {
        public const string SpritesFolder = "Assets/Textures/UI/";

        public enum ButtonStyle
        {
            Primary,
            Secondary,
            Ghost,
            Dark
        }

        public static Sprite Rounded => LoadSprite("ui_arredondado");
        public static Sprite Circle => LoadSprite("ui_circulo");

        public static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + name + ".png");
            if (sprite == null)
                Debug.LogWarning($"[MontAR] Sprite não encontrado: {SpritesFolder}{name}.png (rode tools/gerar_icones_ui.py).");
            return sprite;
        }

        // ------------------------------------------------------------ objetos e layout

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Faixa presa no topo (y para baixo a partir do topo), com altura fixa.</summary>
        public static RectTransform TopBand(RectTransform rect, float top, float height, float left = 0f, float right = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Faixa presa embaixo; a altura vem do ContentSizeFitter.</summary>
        public static RectTransform BottomBand(RectTransform rect, float bottom, float left = 0f, float right = 0f)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + 100f);
            return rect;
        }

        public static Image Img(GameObject go, Color color, Sprite sprite = null, float cornerScale = 1f)
        {
            // GetComponent devolve um "null falso" no Editor: nada de ?? com objetos da Unity.
            var image = go.GetComponent<Image>();
            if (image == null)
                image = go.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = cornerScale;
            }
            return image;
        }

        /// <summary>Cartão arredondado. <paramref name="radius"/> em unidades da tela de referência (1080 de largura).</summary>
        public static Image Card(GameObject go, Color color, float radius = 32f)
        {
            return Img(go, color, Rounded, 48f / Mathf.Max(1f, radius));
        }

        public static VerticalLayoutGroup VLayout(GameObject go, RectOffset padding, float spacing, TextAnchor alignment = TextAnchor.UpperLeft,
            bool controlHeight = true, bool expandWidth = true)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding ?? new RectOffset();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, RectOffset padding, float spacing, TextAnchor alignment = TextAnchor.MiddleLeft,
            bool controlWidth = true, bool controlHeight = true)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding ?? new RectOffset();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = controlWidth;
            layout.childControlHeight = controlHeight;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static LayoutElement Layout(GameObject go, float preferredHeight = -1f, float flexibleHeight = -1f, float preferredWidth = -1f,
            float flexibleWidth = -1f, float minHeight = -1f, float minWidth = -1f)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null)
                element = go.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = flexibleHeight;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = minHeight;
            element.minWidth = minWidth;
            return element;
        }

        public static ContentSizeFitter FitHeight(GameObject go, bool width = false)
        {
            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = width ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return fitter;
        }

        public static RectOffset Pad(int all) => new RectOffset(all, all, all, all);

        public static RectOffset Pad(int horizontal, int vertical) => new RectOffset(horizontal, horizontal, vertical, vertical);

        public static RectOffset Pad(int left, int right, int top, int bottom) => new RectOffset(left, right, top, bottom);

        public static RectTransform Spacer(Transform parent, float flexibleHeight = 1f, float height = -1f)
        {
            RectTransform spacer = Node("Espaço", parent);
            Layout(spacer.gameObject, preferredHeight: height, flexibleHeight: flexibleHeight);
            return spacer;
        }

        // ------------------------------------------------------------ texto

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            RectTransform rect = Node(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            label.lineSpacing = 4f;
            return label;
        }

        // ------------------------------------------------------------ botões

        public static Button MakeButton(Transform parent, string name, string text, ButtonStyle style, out TextMeshProUGUI label,
            float height = 128f, float fontSize = 40f)
        {
            RectTransform rect = Node(name, parent);
            Color background;
            Color foreground;
            switch (style)
            {
                case ButtonStyle.Primary:
                    background = UiTheme.Primary;
                    foreground = UiTheme.TextOnDark;
                    break;
                case ButtonStyle.Secondary:
                    background = UiTheme.Border;
                    foreground = UiTheme.TextPrimary;
                    break;
                case ButtonStyle.Dark:
                    background = new Color(1f, 1f, 1f, 0.12f);
                    foreground = UiTheme.TextOnDark;
                    break;
                default:
                    background = new Color(1f, 1f, 1f, 0f);
                    foreground = UiTheme.Primary;
                    break;
            }

            Image image = Img(rect.gameObject, background, Rounded, 48f / Mathf.Min(48f, height * 0.5f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            label = Label(rect, "Texto", text, fontSize, foreground, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 24f, 0f, 24f, 0f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            Layout(rect.gameObject, preferredHeight: height, minHeight: height);
            return button;
        }

        /// <summary>Botão quadrado/redondo só com texto curto ou ícone (ex.: ×, +, –).</summary>
        public static Button IconButton(Transform parent, string name, string glyph, Sprite icon, Color background, Color foreground,
            float size, float glyphSize, out TextMeshProUGUI label)
        {
            RectTransform rect = Node(name, parent);
            rect.sizeDelta = new Vector2(size, size);
            Image image = Img(rect.gameObject, background, Circle);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.35f);
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            label = null;
            if (icon != null)
            {
                RectTransform iconRect = Node("Ícone", rect);
                Img(iconRect.gameObject, foreground, icon).raycastTarget = false;
                Stretch(iconRect, size * 0.22f, size * 0.22f, size * 0.22f, size * 0.22f);
            }
            else
            {
                label = Label(rect, "Texto", glyph, glyphSize, foreground, FontStyles.Bold, TextAlignmentOptions.Center);
                Stretch(label.rectTransform);
                label.textWrappingMode = TextWrappingModes.NoWrap;
            }

            Layout(rect.gameObject, preferredHeight: size, preferredWidth: size, minHeight: size, minWidth: size);
            return button;
        }

        /// <summary>Ícone dentro de um círculo colorido.</summary>
        public static Image Badge(Transform parent, string name, Sprite icon, Color circle, Color iconColor, float size, float iconFraction = 0.62f)
        {
            RectTransform rect = Node(name, parent);
            rect.sizeDelta = new Vector2(size, size);
            Img(rect.gameObject, circle, Circle).raycastTarget = false;
            Layout(rect.gameObject, preferredHeight: size, preferredWidth: size, minHeight: size, minWidth: size);

            RectTransform iconRect = Node("Ícone", rect);
            float inset = size * (1f - iconFraction) * 0.5f;
            Stretch(iconRect, inset, inset, inset, inset);
            Image image = Img(iconRect.gameObject, iconColor, icon);
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        // ------------------------------------------------------------ campos

        public static TMP_InputField TextInput(Transform parent, string name, string placeholder, out TextMeshProUGUI text)
        {
            // O próprio campo é a borda; o fundo branco é um filho 3 px para dentro (filhos desenham por cima).
            RectTransform rect = Node(name, parent);
            Image background = Card(rect.gameObject, UiTheme.Border, 26f);
            Layout(rect.gameObject, preferredHeight: 124f, minHeight: 124f);

            RectTransform fill = Node("Fundo", rect);
            Stretch(fill, 3f, 3f, 3f, 3f);
            Card(fill.gameObject, UiTheme.Surface, 24f).raycastTarget = false;

            RectTransform area = Node("Área do texto", rect);
            Stretch(area, 36f, 12f, 36f, 12f);
            area.gameObject.AddComponent<RectMask2D>();

            TextMeshProUGUI hint = Label(area, "Placeholder", placeholder, 44f, UiTheme.Disabled, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Stretch(hint.rectTransform);
            hint.textWrappingMode = TextWrappingModes.NoWrap;
            text = Label(area, "Texto", string.Empty, 44f, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = hint;
            input.pointSize = 44f;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.keyboardType = TouchScreenKeyboardType.ASCIICapable;
            input.caretWidth = 4;
            input.customCaretColor = true;
            input.caretColor = UiTheme.Primary;
            input.selectionColor = new Color(UiTheme.Primary.r, UiTheme.Primary.g, UiTheme.Primary.b, 0.3f);
            var navigation = input.navigation;
            navigation.mode = Navigation.Mode.None;
            input.navigation = navigation;
            return input;
        }

        public static Toggle Checkbox(Transform parent, string name, out TextMeshProUGUI label, out GameObject checkMark)
        {
            RectTransform row = Node(name, parent);
            Image rowBackground = Card(row.gameObject, UiTheme.SurfaceMuted, 24f);
            HLayout(row.gameObject, Pad(28, 28, 22, 22), 28f, TextAnchor.MiddleLeft);

            RectTransform box = Node("Caixa", row);
            Img(box.gameObject, UiTheme.Disabled, Rounded, 48f / 16f).raycastTarget = false;
            Layout(box.gameObject, preferredHeight: 68f, preferredWidth: 68f, minHeight: 68f, minWidth: 68f);
            RectTransform boxFill = Node("Fundo", box);
            Stretch(boxFill, 4f, 4f, 4f, 4f);
            Img(boxFill.gameObject, UiTheme.Surface, Rounded, 48f / 12f).raycastTarget = false;

            RectTransform mark = Node("Marcado", box);
            Stretch(mark);
            Image markImage = Img(mark.gameObject, UiTheme.Primary, Rounded, 48f / 16f);
            markImage.raycastTarget = false;
            RectTransform check = Node("Check", mark);
            Stretch(check, 10f, 10f, 10f, 10f);
            Img(check.gameObject, Color.white, LoadSprite("ui_check")).raycastTarget = false;

            label = Label(row, "Texto", "Item", 36f, UiTheme.TextPrimary, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            Layout(label.gameObject, flexibleWidth: 1f);

            // O sinal de marcado é ligado e desligado pelo ChecklistItemView (sem o fade do Toggle).
            var toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = rowBackground;
            toggle.graphic = null;
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            toggle.isOn = false;
            checkMark = mark.gameObject;
            checkMark.SetActive(false);
            var navigation = toggle.navigation;
            navigation.mode = Navigation.Mode.None;
            toggle.navigation = navigation;
            return toggle;
        }

        /// <summary>Área rolável vertical. Devolve o Content (com VerticalLayoutGroup e ContentSizeFitter).</summary>
        public static RectTransform Scroll(Transform parent, string name, RectOffset padding, float spacing, out ScrollRect scrollRect)
        {
            RectTransform root = Node(name, parent);
            Layout(root.gameObject, flexibleHeight: 1f, minHeight: 200f);
            scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 40f;

            RectTransform viewport = Node("Viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(viewport.gameObject, new Color(1f, 1f, 1f, 0f)).raycastTarget = true;

            RectTransform content = Node("Conteúdo", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VLayout(content.gameObject, padding, spacing);
            FitHeight(content.gameObject);

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }
    }
}
