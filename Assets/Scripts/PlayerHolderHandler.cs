using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe
{
    public class PlayerHolderHandler : MonoBehaviour
    {
        [SerializeField]
        private GameObject _symbolHolderPrefab;

        [SerializeField]
        private PlayerConfigSO _playerConfigSO;

        [SerializeField]
        private PlayerModelsSO _playerModelsSO;
        private readonly List<GameObject> _allPlayerSymbols = new();
        private Board _grid;
        private readonly Func<float, float> _easingFunction = x =>
            (float)-(Math.Cos(Math.PI * x) - 1) / 2;
        private readonly float _fadeDuration = 0.25f;

        public void SetupPlayingFieldReference(Board playingField)
        {
            _grid = playingField;
            _grid.OnTurnEnd += HandleTurnEnd;

            int playerCount = _grid.GameConfig.PlayerAmount;

            float screenWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            for (int i = 0; i < playerCount; i++)
            {
                GameObject overlaySymbol = Instantiate(_symbolHolderPrefab, transform);
                overlaySymbol.GetComponentInChildren<MeshFilter>().mesh = _playerModelsSO
                    .PlayerSymbols[i]
                    .GetComponent<MeshFilter>()
                    .sharedMesh;
                overlaySymbol.GetComponentInChildren<MeshRenderer>().material = _playerModelsSO
                    .PlayerSymbols[i]
                    .GetComponent<MeshRenderer>()
                    .sharedMaterial;
                overlaySymbol.GetComponentInChildren<MeshRenderer>().material.color -= new Color(
                    0,
                    0,
                    0,
                    i == 0 ? 0 : 1
                );
                float x =
                    screenWidth
                    / playerCount
                    * Mathf.Lerp(-playerCount / 2, playerCount / 2, i / (playerCount - 1f));
                overlaySymbol.transform.localPosition = new Vector3(
                    playerCount % 2 == 0 ? x - (x / playerCount) : x,
                    overlaySymbol.transform.localPosition.y
                        - transform.parent.GetComponent<RectTransform>().rect.height / 10,
                    overlaySymbol.transform.localPosition.z
                );

                _allPlayerSymbols.Add(overlaySymbol);
            }
        }

        private void HandleTurnEnd()
        {
            StartCoroutine(
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _allPlayerSymbols[
                        _grid.CurrentPlayer == 1
                            ? _allPlayerSymbols.Count - 1
                            : _grid.CurrentPlayer - 2
                    ]
                        .GetComponentInChildren<MeshRenderer>()
                        .material,
                    x => 1 - _easingFunction(x),
                    _fadeDuration,
                    fieldsToChange: "a"
                )
            );
            StartCoroutine(
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _allPlayerSymbols[_grid.CurrentPlayer - 1]
                        .GetComponentInChildren<MeshRenderer>()
                        .material,
                    _easingFunction,
                    _fadeDuration,
                    fieldsToChange: "a"
                )
            );
        }
    }
}
