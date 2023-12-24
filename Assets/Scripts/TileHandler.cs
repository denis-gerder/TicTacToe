using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileHandler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    [SerializeField] protected GameObject playerPrefab;
    
    [SerializeField] protected PlayerConfigSO playerConfigSO;
    
    private Image _image;
    
    protected bool _gameOver = false;
    
    public static event Action<int> OnPlayerTilePlaced;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
        Grid.OnGameOver += HandleGameOver;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //do not show visual if tile is already occupied
        if(Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        StartCoroutine(FadeInVisual(_image));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        //do not show visual if tile is already occupied
        if(Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        StartCoroutine(FadeOutVisual(_image));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        //return if tile is already occupied or if AI is enabled and it's the AI's turn
        if((GameManager.Instance.IsAIEnabled && GameManager.Instance.currentPlayer != 1) ||  Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] != null || _gameOver) return;
        
        //spawn player tile and set reference to tile in grid
        GameObject playerTile = Instantiate(playerPrefab, transform);
        playerTile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1].Sprite;
        playerTile.GetComponent<Image>().color = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1].Color;
        Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] = playerTile;
        
        StartCoroutine(FadeOutVisual(_image));
        
        //turn ends and next player is set
        int nextPlayer = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount ? GameManager.Instance.currentPlayer + 1 : 1;
        GameManager.Instance.currentPlayer = nextPlayer;
        PlayerPlaced();
    }

    private IEnumerator FadeInVisual(Image image)
    {
        for (float i = 0; i <= 10; i++)
        {
            Color tempColor = image.color;
            tempColor.a = i/10;
            image.color = tempColor;
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    private IEnumerator FadeOutVisual(Image image)
    {
        for (float i = 10; i >= 0; i--)
        {
            Color tempColor = image.color;
            tempColor.a = i/10;
            image.color = tempColor;
            yield return new WaitForSeconds(0.01f);
        }
    }

    protected void PlayerPlaced()
    {
        //get player that placed the tile
        int placingPlayer = GameManager.Instance.currentPlayer - 1 != 0 ? GameManager.Instance.currentPlayer - 1 : GameManager.Instance.PlayerCount;
        OnPlayerTilePlaced?.Invoke(placingPlayer);
    }
    
    protected void HandleGameOver(bool isGameWon, int player)
    {
        _gameOver = true;
    }
}
