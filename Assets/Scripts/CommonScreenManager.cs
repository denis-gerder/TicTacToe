using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicTacToe
{
    public class CommonScreenManager : MonoBehaviour
    {
        [SerializeField]
        protected GameObject _startSelection;

        [SerializeField]
        protected GameObject _backButtonSelection;
        private readonly Func<float, float> _easingFunction = x =>
            x < 0.5 ? 4 * x * x * x : 1 - ((float)Math.Pow((-2 * x) + 2, 3) / 2);

        protected readonly Stack<GameObject> _screenHistory = new();

        private GameObject _backButtonSelectionClone;

        protected void Awake()
        {
            _screenHistory.Push(_startSelection);
            if (_backButtonSelection != null)
            {
                _backButtonSelectionClone = Instantiate(_backButtonSelection, transform);
                _backButtonSelectionClone.SetActive(false);
            }
        }

        public void HandleGoNextSlide(GameObject nextScreen)
        {
            float screenWidth = GetComponent<RectTransform>().rect.width;

            GameObject currentScreen = _screenHistory.Peek();
            StartCoroutine(
                EaseObjectPositionInOneDirection(
                    currentScreen.GetComponent<RectTransform>(),
                    _easingFunction,
                    1f,
                    Vector3.left,
                    Vector3.zero,
                    (int)screenWidth,
                    new Action[] { () => currentScreen.SetActive(false) }
                )
            );

            if (currentScreen != _startSelection)
            {
                StartCoroutine(
                    EaseObjectPositionInOneDirection(
                        _backButtonSelectionClone.GetComponent<RectTransform>(),
                        _easingFunction,
                        seconds: 1f,
                        Vector3.left,
                        Vector3.zero,
                        (int)screenWidth,
                        new Action[] { () => _backButtonSelectionClone.SetActive(false) }
                    )
                );
            }

            StartCoroutine(
                EaseObjectPositionInOneDirection(
                    _backButtonSelection.GetComponent<RectTransform>(),
                    _easingFunction,
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
                    _easingFunction,
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
                    _easingFunction,
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
                    _easingFunction,
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
            {
                StartCoroutine(
                    EaseObjectPositionInOneDirection(
                        _backButtonSelection.GetComponent<RectTransform>(),
                        _easingFunction,
                        seconds: 1f,
                        Vector3.right,
                        Vector3.left * (int)screenWidth,
                        (int)screenWidth,
                        null
                    )
                );
            }

            StartCoroutine(
                EaseObjectPositionInOneDirection(
                    previousScreen.GetComponent<RectTransform>(),
                    _easingFunction,
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
                    easingFunction((currentTime - startingTime) / seconds)
                    * moveByPixels
                    * direction;
                objectTransform.localPosition = startPosition + moveVector;

                yield return 0;
                currentTime = Time.time;
            }
            objectTransform.localPosition = startPosition + (direction * moveByPixels);
            if (methodList?.Length > 0)
            {
                foreach (var method in methodList)
                    method();
            }
        }

        public void ChangeScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void SetActiveAndUnActive(GameObject screen)
        {
            screen.SetActive(!screen.activeSelf);
        }

        public void PauseAndUnpauseGame()
        {
            Time.timeScale *= -1 + 1;
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
