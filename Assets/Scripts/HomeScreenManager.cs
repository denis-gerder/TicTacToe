using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TicTacToe
{
    public class HomeScreenManager : CommonScreenManager
    {
        [SerializeField]
        private GameConfigSO _gameConfigSO;

        [SerializeField]
        private Slider _playerSlider;

        [SerializeField]
        private Slider _boardSizeSlider;

        [SerializeField]
        private Toggle _aiToggle;

        [SerializeField]
        private TMP_Dropdown _difficultySelection;

        public void ReflectSliderOnText(GameObject sliderSelection)
        {
            Slider slider = sliderSelection.GetComponentInChildren<Slider>();
            List<Transform> transforms = new();
            foreach (Transform t in sliderSelection.transform)
                transforms.Add(t);
            TextMeshProUGUI text = transforms
                .Find(transform => transform.gameObject.name.Contains("Dynamic"))
                .GetComponent<TextMeshProUGUI>();
            text.text = slider.value.ToString();
        }

        public void InvertInteractability(TMP_Dropdown dropdown)
        {
            dropdown.interactable = !dropdown.interactable;
        }

        public void SaveConfigState()
        {
            _gameConfigSO.PlayerAmount = (int)_playerSlider.value;
            _gameConfigSO.BoardSize = (int)_boardSizeSlider.value;
            _gameConfigSO.AIEnabled = _aiToggle.isOn;
            _gameConfigSO.AIDifficulty = (AIDifficulty)_difficultySelection.value;
        }
    }
}
