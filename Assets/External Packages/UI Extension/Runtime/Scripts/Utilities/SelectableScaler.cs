///Credit Tomek S
///Sourced from - https://pastebin.com/NXYu37jC

using System.Collections;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Selectable Scalar")]
    [RequireComponent(typeof(Button))]
    public class SelectableScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public AnimationCurve animCurve;

        [Tooltip("Animation speed multiplier")]
        public float speed = 1;

        public Transform target;
        private Vector3 initScale;

        private Selectable selectable;

        public Selectable Target
        {
            get
            {
                if (selectable == null) selectable = GetComponent<Selectable>();

                return selectable;
            }
        }

        // Use this for initialization
        private void Awake()
        {
            if (target == null) target = transform;

            initScale = target.localScale;
        }

        private void OnEnable()
        {
            target.localScale = initScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Target != null && !Target.interactable) return;

            StopCoroutine("ScaleOUT");
            StartCoroutine("ScaleIN");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (Target != null && !Target.interactable) return;

            StopCoroutine("ScaleIN");
            StartCoroutine("ScaleOUT");
        }

        private IEnumerator ScaleIN()
        {
            if (animCurve.keys.Length > 0)
            {
                target.localScale = initScale;
                float t = 0;
                var maxT = animCurve.keys[animCurve.length - 1].time;

                while (t < maxT)
                {
                    t += speed * Time.unscaledDeltaTime;
                    target.localScale = Vector3.one * animCurve.Evaluate(t);
                    yield return null;
                }
            }
        }

        private IEnumerator ScaleOUT()
        {
            if (animCurve.keys.Length > 0)
            {
                //target.localScale = initScale;
                float t = 0;
                var maxT = animCurve.keys[animCurve.length - 1].time;

                while (t < maxT)
                {
                    t += speed * Time.unscaledDeltaTime;
                    target.localScale = Vector3.one * animCurve.Evaluate(maxT - t);
                    yield return null;
                }

                transform.localScale = initScale;
            }
        }
    }
}