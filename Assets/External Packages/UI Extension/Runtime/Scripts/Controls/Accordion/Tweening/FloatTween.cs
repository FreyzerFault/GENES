///Credit ChoMPHi
///Sourced from - http://forum.unity3d.com/threads/accordion-type-layout.271818/

using UnityEngine.Events;

namespace UnityEngine.UI.Extensions.Tweens
{
    public struct FloatTween : ITweenValue
    {
        public class FloatTweenCallback : UnityEvent<float>
        {
        }

        public class FloatFinishCallback : UnityEvent
        {
        }

        private FloatTweenCallback m_Target;
        private FloatFinishCallback m_Finish;

        /// <summary>
        ///     Gets or sets the starting float.
        /// </summary>
        /// <value>The start float.</value>
        public float startFloat { get; set; }

        /// <summary>
        ///     Gets or sets the target float.
        /// </summary>
        /// <value>The target float.</value>
        public float targetFloat { get; set; }

        /// <summary>
        ///     Gets or sets the duration of the tween.
        /// </summary>
        /// <value>The duration.</value>
        public float duration { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="UnityEngine.UI.Tweens.ColorTween" /> should ignore time
        ///     scale.
        /// </summary>
        /// <value><c>true</c> if ignore time scale; otherwise, <c>false</c>.</value>
        public bool ignoreTimeScale { get; set; }

        /// <summary>
        ///     Tweens the float based on percentage.
        /// </summary>
        /// <param name="floatPercentage">Float percentage.</param>
        public void TweenValue(float floatPercentage)
        {
            if (!ValidTarget()) return;

            m_Target.Invoke(Mathf.Lerp(startFloat, targetFloat, floatPercentage));
        }

        /// <summary>
        ///     Adds a on changed callback.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void AddOnChangedCallback(UnityAction<float> callback)
        {
            if (m_Target == null) m_Target = new FloatTweenCallback();

            m_Target.AddListener(callback);
        }

        /// <summary>
        ///     Adds a on finish callback.
        /// </summary>
        /// <param name="callback">Callback.</param>
        public void AddOnFinishCallback(UnityAction callback)
        {
            if (m_Finish == null) m_Finish = new FloatFinishCallback();

            m_Finish.AddListener(callback);
        }

        public bool GetIgnoreTimescale() => ignoreTimeScale;

        public float GetDuration() => duration;

        public bool ValidTarget() => m_Target != null;

        /// <summary>
        ///     Invokes the on finish callback.
        /// </summary>
        public void Finished()
        {
            if (m_Finish != null) m_Finish.Invoke();
        }
    }
}