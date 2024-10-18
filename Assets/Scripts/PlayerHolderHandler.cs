using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerHolderHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject _symbolHolderPrefab;

    [SerializeField]
    private PlayerConfigSO _playerConfigSO;

    private List<GameObject> allPlayerSymbols = new();

    private Grid _grid;

    private readonly Func<float, float> _easingFunction = x =>
        (float)-(Math.Cos(Math.PI * x) - 1) / 2;

    private readonly float _fadeDuration = 0.25f;

    public void SetupPlayingFieldReference(Grid playingField)
    {
        _grid = playingField;
        _grid.OnTurnEnd += HandleTurnEnd;

        int playerCount = GameManager.Instance.PlayerCount;

        float screenWidth = transform.parent.GetComponent<RectTransform>().rect.width;
        for (int i = 0; i < playerCount; i++)
        {
            GameObject symbolHolder = Instantiate(_symbolHolderPrefab, transform);
            symbolHolder.transform.GetChild(1).GetComponent<Image>().sprite =
                _playerConfigSO.PlayerSymbols[i];
            RectTransform rect = symbolHolder.GetComponent<RectTransform>();
            float x =
                screenWidth
                / playerCount
                * Mathf.Lerp(-playerCount / 2, playerCount / 2, i / (playerCount - 1f));
            rect.localPosition = new Vector3(
                playerCount % 2 == 0 ? x - x / playerCount : x,
                rect.localPosition.y,
                rect.localPosition.z
            );

            TMP_Text symbolText = symbolHolder.transform.GetChild(2).GetComponent<TMP_Text>();
            if (i == 0)
            {
                Image image = symbolHolder.transform.GetChild(0).GetComponent<Image>();
                StartCoroutine(
                    TileHandler.FadeInVisual(image, _easingFunction, _fadeDuration, 100)
                );
                symbolText.text = "Player: 1";
            }
            else
            {
                symbolText.text = GameManager.Instance._gameConfigSO.AIEnabled
                    ? "AI: " + i
                    : "Player: " + (i + 1);
            }

            allPlayerSymbols.Add(symbolHolder);
        }
    }

    private List<Image> _symbolImages = new();

    private void HandleTurnEnd()
    {
        if (_symbolImages.Count == 0)
            allPlayerSymbols.ForEach(
                (symbol) => _symbolImages.Add(symbol.transform.GetChild(0).GetComponent<Image>())
            );

        for (int i = 0; i < _symbolImages.Count; i++)
        {
            Image image = _symbolImages[i];
            if (i + 1 == _grid.CurrentPlayer)
                StartCoroutine(
                    TileHandler.FadeInVisual(image, _easingFunction, _fadeDuration, 100)
                );
            else if (image.color.a > 0)
                StartCoroutine(
                    TileHandler.FadeOutVisual(image, _easingFunction, _fadeDuration, 100)
                );
        }
    }
}
