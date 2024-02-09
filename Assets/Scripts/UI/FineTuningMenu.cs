using System.Collections.Generic;
using PathFinding;
using PathFinding.A_Star;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class FineTuningMenu : MonoBehaviour
    {
        [SerializeField] private PathFindingConfigSO config;

        [SerializeField] private Button updateButton;

        [SerializeField] private GameObject[] parameterControllerObjects;

        private TMP_Text[] _labelTexts;
        private Dictionary<AstarParam, ParameterController> _parameterControllers;
        private Slider[] _sliders;
        private TMP_Text[] _valueTexts;


        private void Awake()
        {
            _parameterControllers = new Dictionary<AstarParam, ParameterController>();
            foreach (var pair in config.aStarConfig.parameters.pairElements)
                _parameterControllers.Add(pair.key, new ParameterController
                {
                    parent = parameterControllerObjects[(int)pair.key],
                    labelText = parameterControllerObjects[(int)pair.key].GetComponentsInChildren<TMP_Text>()[0],
                    valueText = parameterControllerObjects[(int)pair.key].GetComponentsInChildren<TMP_Text>()[1],
                    slider = parameterControllerObjects[(int)pair.key].GetComponentInChildren<Slider>()
                });
        }

        private void Start()
        {
            foreach (var controller in _parameterControllers)
            {
                var paramName = controller.Key.ToString();
                var value = config.aStarConfig.parameters.GetValue(controller.Key);

                controller.Value.labelText.text = paramName;
                controller.Value.valueText.text = value.ToString("F2");
                controller.Value.slider.value = value;
                controller.Value.slider.onValueChanged.AddListener(newValue => OnValueChange(newValue, controller.Key));
            }
        }

        private void OnValueChange(float value, AstarParam parameter)
        {
            config.aStarConfig.parameters.SetValue(parameter, value);
            updateButton.gameObject.SetActive(true);

            _parameterControllers[parameter].valueText.text = value.ToString("F2");
        }

        public void OnUpdateConfirmed()
        {
            config.OnFineTuneTriggerEvent();
            updateButton.gameObject.SetActive(false);
        }

        private struct ParameterController
        {
            public GameObject parent;
            public TMP_Text labelText;
            public TMP_Text valueText;
            public Slider slider;
        }
    }
}