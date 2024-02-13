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
        [SerializeField] private PathFindingConfigSo pathFindingConfig;

        [SerializeField] private Button updateButton;

        [SerializeField] private GameObject[] parameterControllerObjects;

        private TMP_Text[] _labelTexts;
        private Dictionary<AstarParam, ParameterController> _parameterControllers;
        private Slider[] _sliders;
        private TMP_Text[] _valueTexts;

        private AstarConfig Config => pathFindingConfig.Config;


        private void Awake()
        {
            _parameterControllers = new Dictionary<AstarParam, ParameterController>();
            foreach (var pair in Config.parameters.pairElements)
                _parameterControllers.Add(pair.key, new ParameterController
                {
                    labelText = parameterControllerObjects[(int)pair.key].GetComponentsInChildren<TMP_Text>()[0],
                    valueText = parameterControllerObjects[(int)pair.key].GetComponentsInChildren<TMP_Text>()[1],
                    slider = parameterControllerObjects[(int)pair.key].GetComponentInChildren<Slider>()
                });
        }

        private void Start()
        {
            foreach (var controller in _parameterControllers)
            {
                var value = Config.parameters.GetValue(controller.Key).value;
                var displayName = Config.parameters.GetValue(controller.Key).displayName;

                controller.Value.labelText.text = displayName;
                controller.Value.valueText.text = value.ToString("F2");
                controller.Value.slider.value = value;
                controller.Value.slider.onValueChanged.AddListener(newValue =>
                    OnValueChange(newValue, displayName, controller.Key));
            }
        }

        private void OnValueChange(float value, string displayName, AstarParam parameter)
        {
            Config.parameters.SetValue(parameter,
                new ParamValue { value = value, displayName = displayName });
            updateButton.gameObject.SetActive(true);

            _parameterControllers[parameter].valueText.text = value.ToString("F2");
        }

        public void OnUpdateConfirmed()
        {
            pathFindingConfig.OnFineTuneTriggerEvent();
            updateButton.gameObject.SetActive(false);
        }

        private struct ParameterController
        {
            public TMP_Text labelText;
            public TMP_Text valueText;
            public Slider slider;
        }
    }
}