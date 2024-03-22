///Credit ChoMPHi
///Sourced from - http://forum.unity3d.com/threads/accordion-type-layout.271818/

using System;
using System.Collections;
using UnityEngine.UI.Extensions.Tweens;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(RectTransform), typeof(LayoutElement))]
    [AddComponentMenu("UI/Extensions/Accordion/Accordion Element")]
    public class AccordionElement : Toggle
    {
        [SerializeField] private float m_MinHeight = 18f;

        [SerializeField] private float m_MinWidth = 40f;

        [NonSerialized] private readonly TweenRunner<FloatTween> m_FloatTweenRunner;

        private Accordion m_Accordion;
        private LayoutElement m_LayoutElement;
        private RectTransform m_RectTransform;

        protected AccordionElement()
        {
            if (m_FloatTweenRunner == null) m_FloatTweenRunner = new TweenRunner<FloatTween>();

            m_FloatTweenRunner.Init(this);
        }

        public float MinHeight => m_MinHeight;

        public float MinWidth => m_MinWidth;

        protected override void Awake()
        {
            base.Awake();
            transition = Transition.None;
            toggleTransition = ToggleTransition.None;
            m_Accordion = gameObject.GetComponentInParent<Accordion>();
            m_RectTransform = transform as RectTransform;
            m_LayoutElement = gameObject.GetComponent<LayoutElement>();
            onValueChanged.AddListener(OnValueChanged);
        }

        private new IEnumerator Start()
        {
            base.Start();
            yield return new WaitForEndOfFrame(); // Wait for the first frame
            OnValueChanged(isOn);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Accordion = gameObject.GetComponentInParent<Accordion>();

            if (group == null)
            {
                var tg = GetComponentInParent<ToggleGroup>();

                if (tg != null) group = tg;
            }

            var le = gameObject.GetComponent<LayoutElement>();

            if (le != null && m_Accordion != null)
            {
                if (isOn)
                {
                    if (m_Accordion.ExpandVerticval)
                        le.preferredHeight = -1f;
                    else
                        le.preferredWidth = -1f;
                }
                else
                {
                    if (m_Accordion.ExpandVerticval)
                        le.preferredHeight = m_MinHeight;
                    else
                        le.preferredWidth = m_MinWidth;
                }
            }
        }
#endif

        public void OnValueChanged(bool state)
        {
            if (m_LayoutElement == null) return;

            var transition = m_Accordion != null ? m_Accordion.transition : Accordion.Transition.Instant;

            if (transition == Accordion.Transition.Instant && m_Accordion != null)
            {
                if (state)
                {
                    if (m_Accordion.ExpandVerticval)
                        m_LayoutElement.preferredHeight = -1f;
                    else
                        m_LayoutElement.preferredWidth = -1f;
                }
                else
                {
                    if (m_Accordion.ExpandVerticval)
                        m_LayoutElement.preferredHeight = m_MinHeight;
                    else
                        m_LayoutElement.preferredWidth = m_MinWidth;
                }
            }
            else if (transition == Accordion.Transition.Tween)
            {
                if (state)
                {
                    if (m_Accordion.ExpandVerticval)
                        StartTween(m_MinHeight, GetExpandedHeight());
                    else
                        StartTween(m_MinWidth, GetExpandedWidth());
                }
                else
                {
                    if (m_Accordion.ExpandVerticval)
                        StartTween(m_RectTransform.rect.height, m_MinHeight);
                    else
                        StartTween(m_RectTransform.rect.width, m_MinWidth);
                }
            }
        }

        protected float GetExpandedHeight()
        {
            if (m_LayoutElement == null) return m_MinHeight;

            var originalPrefH = m_LayoutElement.preferredHeight;
            m_LayoutElement.preferredHeight = -1f;
            var h = LayoutUtility.GetPreferredHeight(m_RectTransform);
            m_LayoutElement.preferredHeight = originalPrefH;

            return h;
        }

        protected float GetExpandedWidth()
        {
            if (m_LayoutElement == null) return m_MinWidth;

            var originalPrefW = m_LayoutElement.preferredWidth;
            m_LayoutElement.preferredWidth = -1f;
            var w = LayoutUtility.GetPreferredWidth(m_RectTransform);
            m_LayoutElement.preferredWidth = originalPrefW;

            return w;
        }

        protected void StartTween(float startFloat, float targetFloat)
        {
            var duration = m_Accordion != null ? m_Accordion.transitionDuration : 0.3f;

            var info = new FloatTween
            {
                duration = duration,
                startFloat = startFloat,
                targetFloat = targetFloat
            };
            if (m_Accordion.ExpandVerticval)
                info.AddOnChangedCallback(SetHeight);
            else
                info.AddOnChangedCallback(SetWidth);
            info.ignoreTimeScale = true;
            m_FloatTweenRunner.StartTween(info);
        }

        protected void SetHeight(float height)
        {
            if (m_LayoutElement == null) return;

            m_LayoutElement.preferredHeight = height;
        }

        protected void SetWidth(float width)
        {
            if (m_LayoutElement == null) return;

            m_LayoutElement.preferredWidth = width;
        }
    }
}