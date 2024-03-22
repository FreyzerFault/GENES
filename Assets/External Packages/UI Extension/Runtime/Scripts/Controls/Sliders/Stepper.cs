/// Credit David Gileadi
/// Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/pull-requests/11

using System;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    // Stepper control
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("UI/Extensions/Sliders/Stepper")]
    public class Stepper : UIBehaviour
    {
        [SerializeField] [Tooltip("The current step value of the control")]
        private int _value;

        [SerializeField]
        [Tooltip("The minimum step value allowed by the control. When reached it will disable the '-' button")]
        private int _minimum;

        [SerializeField]
        [Tooltip("The maximum step value allowed by the control. When reached it will disable the '+' button")]
        private int _maximum = 100;

        [SerializeField] [Tooltip("The step increment used to increment / decrement the step value")]
        private int _step = 1;

        [SerializeField] [Tooltip("Does the step value loop around from end to end")]
        private bool _wrap;

        [SerializeField]
        [Tooltip(
            "A GameObject with an Image to use as a separator between segments. Size of the RectTransform will determine the size of the separator used.\nNote, make sure to disable the separator GO so that it does not affect the scene"
        )]
        private Graphic _separator;

        // Event delegates triggered on click.
        [SerializeField] private StepperValueChangedEvent _onValueChanged = new();

        private float _separatorWidth;
        private Selectable[] _sides;

        protected Stepper()
        {
        }

        private float separatorWidth
        {
            get
            {
                if (_separatorWidth == 0 && separator)
                {
                    _separatorWidth = separator.rectTransform.rect.width;
                    var image = separator.GetComponent<Image>();
                    if (image) _separatorWidth /= image.pixelsPerUnit;
                }

                return _separatorWidth;
            }
        }

        public Selectable[] sides
        {
            get
            {
                if (_sides == null || _sides.Length == 0) _sides = GetSides();
                return _sides;
            }
        }

        public int value
        {
            get => _value;
            set => _value = value;
        }

        public int minimum
        {
            get => _minimum;
            set => _minimum = value;
        }

        public int maximum
        {
            get => _maximum;
            set => _maximum = value;
        }

        public int step
        {
            get => _step;
            set => _step = value;
        }

        public bool wrap
        {
            get => _wrap;
            set => _wrap = value;
        }

        public Graphic separator
        {
            get => _separator;
            set
            {
                _separator = value;
                _separatorWidth = 0;
                LayoutSides(sides);
            }
        }

        public StepperValueChangedEvent onValueChanged
        {
            get => _onValueChanged;
            set => _onValueChanged = value;
        }

        protected override void Start()
        {
            if (isActiveAndEnabled) StartCoroutine(DelayedInit());
        }

        protected override void OnEnable()
        {
            StartCoroutine(DelayedInit());
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            RecreateSprites(sides);
            if (separator) LayoutSides();

            if (!wrap) DisableAtExtremes(sides);
        }
#endif

        private IEnumerator DelayedInit()
        {
            yield return null;

            RecreateSprites(sides);
        }

        private Selectable[] GetSides()
        {
            var buttons = GetComponentsInChildren<Selectable>();
            if (buttons.Length != 2) throw new InvalidOperationException("A stepper must have two Button children");

            if (!wrap) DisableAtExtremes(buttons);
            LayoutSides(buttons);

            return buttons;
        }

        public void StepUp()
        {
            Step(step);
        }

        public void StepDown()
        {
            Step(-step);
        }

        private void Step(int amount)
        {
            value += amount;

            if (wrap)
            {
                if (value > maximum) value = minimum;
                if (value < minimum) value = maximum;
            }
            else
            {
                value = Math.Max(minimum, value);
                value = Math.Min(maximum, value);

                DisableAtExtremes(sides);
            }

            _onValueChanged.Invoke(value);
        }

        private void DisableAtExtremes(Selectable[] sides)
        {
            sides[0].interactable = wrap || value > minimum;
            sides[1].interactable = wrap || value < maximum;
        }

        private void RecreateSprites(Selectable[] sides)
        {
            for (var i = 0; i < 2; i++)
            {
                if (sides[i].image == null) continue;

                var sprite = CutSprite(sides[i].image.sprite, i == 0);
                var side = sides[i].GetComponent<StepperSide>();
                if (side) side.cutSprite = sprite;
                sides[i].image.overrideSprite = sprite;
            }
        }

        internal static Sprite CutSprite(Sprite sprite, bool leftmost)
        {
            if (sprite.border.x == 0 || sprite.border.z == 0) return sprite;

            var rect = sprite.rect;
            var border = sprite.border;

            if (leftmost)
            {
                rect.xMax = border.z;
                border.z = 0;
            }
            else
            {
                rect.xMin = border.x;
                border.x = 0;
            }

            return Sprite.Create(
                sprite.texture,
                rect,
                sprite.pivot,
                sprite.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                border
            );
        }

        public void LayoutSides(Selectable[] sides = null)
        {
            sides = sides ?? this.sides;

            RecreateSprites(sides);

            var transform = this.transform as RectTransform;
            var width = transform.rect.width / 2 - separatorWidth;

            for (var i = 0; i < 2; i++)
            {
                var insetX = i == 0 ? 0 : width + separatorWidth;

                var rectTransform = sides[i].GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, insetX, width);
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, transform.rect.height);

// TODO: maybe adjust text position
            }

            if (separator)
            {
                var sepTransform = gameObject.transform.Find("Separator");
                var sep = sepTransform != null
                    ? sepTransform.GetComponent<Graphic>()
                    : Instantiate(separator.gameObject).GetComponent<Graphic>();
                sep.gameObject.name = "Separator";
                sep.gameObject.SetActive(true);
                sep.rectTransform.SetParent(this.transform, false);
                sep.rectTransform.anchorMin = Vector2.zero;
                sep.rectTransform.anchorMax = Vector2.zero;
                sep.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, width, separatorWidth);
                sep.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, transform.rect.height);
            }
        }

        [Serializable]
        public class StepperValueChangedEvent : UnityEvent<int>
        {
        }
    }
}