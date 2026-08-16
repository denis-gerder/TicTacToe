using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicTacToe
{
    public class CommonScreenManager : MonoBehaviour
    {
        [SerializeField]
        protected GameObject _startScreen;

        protected readonly Stack<GameObject> _screenHistory = new();
        protected GameObject _currentScreen;
        protected CoroutineManager _coroutineManager;
        private readonly Func<float, float> _easingFunction = x =>
            x < 0.5 ? 4 * x * x * x : 1 - ((float)Math.Pow((-2 * x) + 2, 3) / 2);

        protected void Start()
        {
            _coroutineManager = new(this);
            _currentScreen = _startScreen;
        }

        public void HandleGoNextSlide(GameObject nextScreen)
        {
            float screenWidth = gameObject.GetComponent<RectTransform>().rect.width;

            RectTransform nextScreenTransform = nextScreen.GetComponent<RectTransform>();

            List<IEnumerator> parallelCoroutines = new();

            if (_currentScreen != null)
            {
                RectTransform currentScreenTransform = _currentScreen.GetComponent<RectTransform>();
                parallelCoroutines.Add(
                    AnimationUtils.EasePropertyFloatOnObject(
                        currentScreenTransform,
                        "localPosition",
                        _easingFunction,
                        1f,
                        0,
                        -(int)screenWidth,
                        "x"
                    )
                );
            }

            parallelCoroutines.Add(
                AnimationUtils.ActionAsCoroutine(() => nextScreen.SetActive(true))
            );
            parallelCoroutines.Add(
                AnimationUtils.EasePropertyFloatOnObject(
                    nextScreenTransform,
                    "localPosition",
                    _easingFunction,
                    1f,
                    (int)screenWidth,
                    0,
                    "x"
                )
            );

            _coroutineManager.EnqueueParallel(parallelCoroutines.ToArray());
            if (_currentScreen != null)
                _coroutineManager.EnqueueSequentally(
                    AnimationUtils.ActionAsCoroutine(() => _currentScreen.SetActive(false)),
                    AnimationUtils.ActionAsCoroutine(() => _screenHistory.Push(_currentScreen))
                );
            _coroutineManager.EnqueueSequentally(
                AnimationUtils.ActionAsCoroutine(() => _currentScreen = nextScreen)
            );
        }

        public void GoBackOneSlide()
        {
            float screenWidth = gameObject.GetComponent<RectTransform>().rect.width;
            GameObject previousScreen = null;
            List<IEnumerator> parallelCoroutines = new();
            if (_screenHistory.Count > 0)
            {
                previousScreen = _screenHistory.Pop();
                RectTransform previousScreenTransform =
                    previousScreen.GetComponent<RectTransform>();
                parallelCoroutines.Add(
                    AnimationUtils.ActionAsCoroutine(() => previousScreen.SetActive(true))
                );
                parallelCoroutines.Add(
                    AnimationUtils.EasePropertyFloatOnObject(
                        previousScreenTransform,
                        "localPosition",
                        _easingFunction,
                        1f,
                        -(int)screenWidth,
                        0,
                        "x"
                    )
                );
            }

            RectTransform currentScreenTransform = _currentScreen.GetComponent<RectTransform>();

            parallelCoroutines.Add(
                AnimationUtils.EasePropertyFloatOnObject(
                    currentScreenTransform,
                    "localPosition",
                    _easingFunction,
                    1f,
                    0,
                    (int)screenWidth,
                    "x"
                )
            );
            _coroutineManager
                .EnqueueParallel(parallelCoroutines.ToArray())
                .EnqueueSequentally(
                    AnimationUtils.ActionAsCoroutine(() => _currentScreen.SetActive(false)),
                    AnimationUtils.ActionAsCoroutine(() => _currentScreen = previousScreen)
                );
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
