using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TicTacToe
{
    public class TileHandler
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerDownHandler,
            IPointerExitHandler
    {
        [SerializeField]
        private GameObject _playerPrefab;

        public PlayerConfigSO PlayerConfigSo;

        private Image _mouseOverVisual;

        //Opacity of the Mouse Over Visual in percent
        private readonly int _maximumMouseOverOpacity = 50;

        private readonly float _fadeDuration = 0.25f;

        private readonly Func<float, float> _easingFunction = x =>
            (float)-(Math.Cos(Math.PI * x) - 1) / 2;

        public static event Action<int> OnPlayerTilePlaced;

        private Grid _playingField;

        private void Awake()
        {
            _mouseOverVisual = gameObject.transform.GetChild(0).GetComponent<Image>();
        }

        public void SetupPlayingFieldReference(Grid playingField)
        {
            _playingField = playingField;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //do not show visual if tile is already occupied
            if (_playingField.PlayerPerTile[gameObject] != null)
                return;
            StopAllCoroutines();
            StartCoroutine(
                FadeInVisual(
                    _mouseOverVisual,
                    _easingFunction,
                    _fadeDuration,
                    _maximumMouseOverOpacity
                )
            );
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //do not show visual if tile is already occupied
            if (_playingField.PlayerPerTile[gameObject] != null)
                return;
            StopAllCoroutines();
            StartCoroutine(
                FadeOutVisual(
                    _mouseOverVisual,
                    _easingFunction,
                    _fadeDuration,
                    _maximumMouseOverOpacity
                )
            );
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //return if tile is already occupied or if AI is enabled and it's the AI's turn
            if (
                (_playingField.GameConfig.AIEnabled && _playingField.CurrentPlayer != 1)
                || _playingField.PlayerPerTile[gameObject] != null
                || GameManager.GameOver
            )
            {
                return;
            }

            //spawn player tile and set reference to tile in grid
            PlaceTile(transform);

            StopAllCoroutines();
            StartCoroutine(
                FadeOutVisual(
                    _mouseOverVisual,
                    _easingFunction,
                    _fadeDuration,
                    _maximumMouseOverOpacity
                )
            );
        }

        public void PlaceTile(Transform parentTransform)
        {
            GameObject playerTile = Instantiate(_playerPrefab, parentTransform);
            playerTile.GetComponent<Image>().sprite = PlayerConfigSo.PlayerSymbols[
                _playingField.CurrentPlayer - 1
            ];
            _playingField.PlayerPerTile[gameObject] = playerTile;
            OnPlayerTilePlaced?.Invoke(_playingField.CurrentPlayer);
        }

        public static IEnumerator FadeInVisual(
            Image image,
            Func<float, float> easingFunction,
            float fadeDuration,
            float maximumOpacity
        )
        {
            float startingTime = Time.time;
            float currentTime = startingTime;
            Color tempColor = image.color;
            while (currentTime - fadeDuration <= startingTime)
            {
                tempColor.a =
                    easingFunction((currentTime - startingTime) / fadeDuration)
                    * maximumOpacity
                    / 100f;
                image.color = tempColor;

                yield return 0;
                currentTime = Time.time;
            }
            tempColor.a = maximumOpacity;
            image.color = tempColor;
        }

        public static IEnumerator FadeOutVisual(
            Image image,
            Func<float, float> easingFunction,
            float fadeDuration,
            float maximumOpacity
        )
        {
            float startingTime = Time.time;
            float currentTime = startingTime;
            Color tempColor = image.color;
            while (currentTime - fadeDuration <= startingTime)
            {
                tempColor.a =
                    (maximumOpacity / 100f)
                    - (
                        easingFunction((currentTime - startingTime) / fadeDuration)
                        * maximumOpacity
                        / 100f
                    );
                image.color = tempColor;

                yield return 0;
                currentTime = Time.time;
            }
            tempColor.a = 0;
            image.color = tempColor;
        }
    }
}
