using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeScreenManager : MonoBehaviour
{
    //Singleton reference
    public static HomeScreenManager Instance;

    [SerializeField]
    private GameObject _startSelection;

    [SerializeField]
    private GameObject _backButtonSelection;

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

    private GameObject _backButtonSelectionClone;

    Func<float, float> easingFunction = x =>
        x < 0.5 ? 4 * x * x * x : 1 - (float)Math.Pow(-2 * x + 2, 3) / 2;

    private Stack<GameObject> _screenHistory = new();

    void Awake()
    {
        Instance = this;
        _screenHistory.Push(_startSelection);
        _backButtonSelectionClone = Instantiate(_backButtonSelection, transform);
        _backButtonSelectionClone.SetActive(false);
    }

    public void HandleGoNextSlide(GameObject nextScreen)
    {
        float screenWidth = GetComponent<RectTransform>().rect.width;

        GameObject currentScreen = _screenHistory.Peek();
        StartCoroutine(
            EaseObjectPositionInOneDirection(
                currentScreen.GetComponent<RectTransform>(),
                easingFunction,
                1f,
                Vector3.left,
                Vector3.zero,
                (int)screenWidth,
                new Action[] { () => currentScreen.SetActive(false) }
            )
        );

        if (currentScreen != _startSelection)
            StartCoroutine(
                EaseObjectPositionInOneDirection(
                    _backButtonSelectionClone.GetComponent<RectTransform>(),
                    easingFunction,
                    seconds: 1f,
                    Vector3.left,
                    Vector3.zero,
                    (int)screenWidth,
                    new Action[] { () => _backButtonSelectionClone.SetActive(false) }
                )
            );

        StartCoroutine(
            EaseObjectPositionInOneDirection(
                _backButtonSelection.GetComponent<RectTransform>(),
                easingFunction,
                seconds: 1f,
                Vector3.left,
                Vector3.right * (int)screenWidth,
                (int)screenWidth,
                null
            )
        );
        StartCoroutine(
            EaseObjectPositionInOneDirection(
                nextScreen.GetComponent<RectTransform>(),
                easingFunction,
                seconds: 1f,
                Vector3.left,
                Vector3.right * (int)screenWidth,
                (int)screenWidth,
                null
            )
        );
        _screenHistory.Push(nextScreen);
    }

    public void GoBackOneSlide()
    {
        float screenWidth = GetComponent<RectTransform>().rect.width;

        GameObject currentScreen = _screenHistory.Pop();
        StartCoroutine(
            EaseObjectPositionInOneDirection(
                currentScreen.GetComponent<RectTransform>(),
                easingFunction,
                1f,
                Vector3.right,
                Vector3.zero,
                (int)screenWidth,
                new Action[] { () => currentScreen.SetActive(false) }
            )
        );
        StartCoroutine(
            EaseObjectPositionInOneDirection(
                _backButtonSelectionClone.GetComponent<RectTransform>(),
                easingFunction,
                seconds: 1f,
                Vector3.right,
                Vector3.zero,
                (int)screenWidth,
                new Action[] { () => _backButtonSelectionClone.SetActive(false) }
            )
        );

        GameObject previousScreen = _screenHistory.Peek();

        if (previousScreen == _startSelection)
        {
            _backButtonSelection.SetActive(false);
            _backButtonSelection.GetComponent<RectTransform>().localPosition = Vector3.zero;
        }
        else
            StartCoroutine(
                EaseObjectPositionInOneDirection(
                    _backButtonSelection.GetComponent<RectTransform>(),
                    easingFunction,
                    seconds: 1f,
                    Vector3.right,
                    Vector3.left * (int)screenWidth,
                    (int)screenWidth,
                    null
                )
            );

        StartCoroutine(
            EaseObjectPositionInOneDirection(
                previousScreen.GetComponent<RectTransform>(),
                easingFunction,
                seconds: 1f,
                Vector3.right,
                Vector3.left * (int)screenWidth,
                (int)screenWidth,
                null
            )
        );
    }

    private IEnumerator EaseObjectPositionInOneDirection(
        Transform objectTransform,
        Func<float, float> easingFunction,
        float seconds,
        Vector3 direction,
        Vector3 startPosition,
        int moveByPixels,
        Action[] methodList
    )
    {
        float startingTime = Time.time;
        float currentTime = startingTime;
        objectTransform.localPosition = startPosition;
        objectTransform.gameObject.SetActive(true);
        while (currentTime - seconds <= startingTime)
        {
            Vector3 moveVector =
                direction * easingFunction((currentTime - startingTime) / seconds) * moveByPixels;
            objectTransform.localPosition = startPosition + moveVector;

            yield return 0;
            currentTime = Time.time;
        }
        objectTransform.localPosition = startPosition + direction * moveByPixels;
        if (methodList != null && methodList.Length > 0)
            foreach (var method in methodList)
                method();
    }

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

    public void ChangeScene(SceneAsset nextScene)
    {
        SceneManager.LoadScene(nextScene.name);
    }

    public void SaveConfigState()
    {
        _gameConfigSO.PlayerAmount = (int)_playerSlider.value;
        _gameConfigSO.BoardSize = (int)_boardSizeSlider.value;
        _gameConfigSO.AIEnabled = _aiToggle.isOn;
        _gameConfigSO.AIDifficulty = (AIDifficulty)_difficultySelection.value;
    }
}
