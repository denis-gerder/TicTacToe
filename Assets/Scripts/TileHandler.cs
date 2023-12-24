using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileHandler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    [SerializeField] private GameObject playerPrefab;
    
    [SerializeField] private PlayerConfigSO playerConfigSO;
    
    private Image _image;
    
    public static event Action<int> OnPlayerTilePlaced;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        StartCoroutine(FadeInVisual(_image));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if(Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        StartCoroutine(FadeOutVisual(_image));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        //return if tile is already occupied
        if(Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        
        //spawn player tile and set reference to tile in grid
        GameObject playerTile = Instantiate(playerPrefab, transform);
        playerTile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1].Sprite;
        playerTile.GetComponent<Image>().color = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1].Color;
        Grid.Instance.PlayerPerTile[gameObject.transform.parent.gameObject] = playerTile;
        
        StartCoroutine(FadeOutVisual(_image));
        
        OnPlayerTilePlaced?.Invoke(GameManager.Instance.currentPlayer);
        
        //switch to next player
        if(GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount) GameManager.Instance.currentPlayer++;
        else GameManager.Instance.currentPlayer = 1;
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
}
