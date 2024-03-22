///Credit ChoMPHi
///Sourced from - http://forum.unity3d.com/threads/accordion-type-layout.271818/


namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(HorizontalOrVerticalLayoutGroup), typeof(ContentSizeFitter), typeof(ToggleGroup))]
    [AddComponentMenu("UI/Extensions/Accordion/Accordion Group")]
    public class Accordion : MonoBehaviour
    {
        public enum Transition
        {
            Instant,
            Tween
        }

        [SerializeField] private Transition m_Transition = Transition.Instant;
        [SerializeField] private float m_TransitionDuration = 0.3f;

        [HideInInspector] public bool ExpandVerticval { get; private set; } = true;

        /// <summary>
        ///     Gets or sets the transition.
        /// </summary>
        /// <value>The transition.</value>
        public Transition transition
        {
            get => m_Transition;
            set => m_Transition = value;
        }

        /// <summary>
        ///     Gets or sets the duration of the transition.
        /// </summary>
        /// <value>The duration of the transition.</value>
        public float transitionDuration
        {
            get => m_TransitionDuration;
            set => m_TransitionDuration = value;
        }

        private void Awake()
        {
            ExpandVerticval = GetComponent<HorizontalLayoutGroup>() ? false : true;
            var group = GetComponent<ToggleGroup>();
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (!GetComponent<HorizontalLayoutGroup>() && !GetComponent<VerticalLayoutGroup>())
                Debug.LogError("Accordion requires either a Horizontal or Vertical Layout group to place children");
        }
#endif
    }
}