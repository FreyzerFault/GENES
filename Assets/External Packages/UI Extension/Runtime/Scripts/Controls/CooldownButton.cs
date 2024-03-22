/// Credit SimonDarksideJ
/// Sourced from my head

using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Cooldown Button")]
    public class CooldownButton : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        #region Update

        // Update is called once per frame
        private void Update()
        {
            if (CooldownActive)
            {
                cooldownTimeRemaining -= Time.deltaTime * cooldownSpeed;
                cooldownTimeElapsed = CooldownTimeout - CooldownTimeRemaining;
                if (cooldownTimeRemaining < 0)
                {
                    StopCooldown();
                }
                else
                {
                    cooldownPercentRemaining = (int)(100 * cooldownTimeRemaining * CooldownTimeout / 100);
                    cooldownPercentComplete = (int)((CooldownTimeout - cooldownTimeRemaining) / CooldownTimeout * 100);
                }
            }
        }

        #endregion

        #region IPointerDownHandler

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            HandleButtonClick(eventData);
        }

        #endregion

        #region ISubmitHandler

        public void OnSubmit(BaseEventData eventData)
        {
            HandleButtonClick(eventData);
        }

        #endregion ISubmitHandler

        #region Private Methods

        public void HandleButtonClick(BaseEventData eventData)
        {
            buttonSource = eventData;

            if (CooldownInEffect) OnButtonClickDuringCooldown?.Invoke(buttonSource.selectedObject);
            if (!CooldownInEffect)
            {
                OnCooldownStart?.Invoke(buttonSource.selectedObject);
                cooldownTimeRemaining = cooldownTimeout;
                cooldownActive = cooldownInEffect = true;
            }
        }

        #endregion Private Methods

        #region Sub-Classes

        [Serializable]
        public class CooldownButtonEvent : UnityEvent<GameObject>
        {
        }

        #endregion

        #region Private variables

        [SerializeField] private float cooldownTimeout;

        [SerializeField] private float cooldownSpeed = 1;

        [SerializeField] [ReadOnly] private bool cooldownActive;

        [SerializeField] [ReadOnly] private bool cooldownInEffect;

        [SerializeField] [ReadOnly] private float cooldownTimeElapsed;

        [SerializeField] [ReadOnly] private float cooldownTimeRemaining;

        [SerializeField] [ReadOnly] private int cooldownPercentRemaining;

        [SerializeField] [ReadOnly] private int cooldownPercentComplete;

        private BaseEventData buttonSource;

        #endregion

        #region Public Properties

        public float CooldownTimeout
        {
            get => cooldownTimeout;
            set => cooldownTimeout = value;
        }

        public float CooldownSpeed
        {
            get => cooldownSpeed;
            set => cooldownSpeed = value;
        }

        public bool CooldownInEffect => cooldownInEffect;

        public bool CooldownActive
        {
            get => cooldownActive;
            set => cooldownActive = value;
        }

        public float CooldownTimeElapsed
        {
            get => cooldownTimeElapsed;
            set => cooldownTimeElapsed = value;
        }

        public float CooldownTimeRemaining => cooldownTimeRemaining;

        public int CooldownPercentRemaining => cooldownPercentRemaining;

        public int CooldownPercentComplete => cooldownPercentComplete;

        #endregion

        #region Events

        [Tooltip("Event that fires when a button is initially pressed down")]
        public CooldownButtonEvent OnCooldownStart;

        [Tooltip("Event that fires when a button is released")]
        public CooldownButtonEvent OnButtonClickDuringCooldown;

        [Tooltip("Event that continually fires while a button is held down")]
        public CooldownButtonEvent OnCoolDownFinish;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Pause Cooldown without resetting values, allows Restarting of cooldown
        /// </summary>
        public void PauseCooldown()
        {
            if (CooldownInEffect) CooldownActive = false;
        }

        /// <summary>
        ///     Restart a paused cooldown
        /// </summary>
        public void RestartCooldown()
        {
            if (CooldownInEffect) CooldownActive = true;
        }

        /// <summary>
        ///     Start a cooldown from outside
        /// </summary>
        public void StartCooldown()
        {
            var emptySource = new BaseEventData(EventSystem.current);
            buttonSource = emptySource;
            OnCooldownStart.Invoke(emptySource.selectedObject);
            cooldownTimeRemaining = cooldownTimeout;
            CooldownActive = cooldownInEffect = true;
        }

        /// <summary>
        ///     Stop a running Cooldown and reset all values
        /// </summary>
        public void StopCooldown()
        {
            cooldownTimeElapsed = CooldownTimeout;
            cooldownTimeRemaining = 0;
            cooldownPercentRemaining = 0;
            cooldownPercentComplete = 100;
            cooldownActive = cooldownInEffect = false;
            OnCoolDownFinish?.Invoke(buttonSource.selectedObject);
        }

        /// <summary>
        ///     Stop a running Cooldown and retain current values
        /// </summary>
        public void CancelCooldown()
        {
            cooldownActive = cooldownInEffect = false;
        }

        #endregion
    }
}