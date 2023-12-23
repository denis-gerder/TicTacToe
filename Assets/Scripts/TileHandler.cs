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
    
    public static event Action<bool> OnGameOver;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Grid.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        StartCoroutine(FadeInVisual(_image));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if(Grid.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        StartCoroutine(FadeOutVisual(_image));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        //return if tile is already occupied
        if(Grid.PlayerPerTile[gameObject.transform.parent.gameObject] != null) return;
        
        //spawn player tile and set reference to tile in grid
        GameObject playerTile = Instantiate(playerPrefab, transform);
        playerTile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.CurrentPlayer - 1].Sprite;
        playerTile.GetComponent<Image>().color = playerConfigSO.PlayerSymbols[GameManager.Instance.CurrentPlayer - 1].Color;
        Grid.PlayerPerTile[gameObject.transform.parent.gameObject] = playerTile;

        if (CheckForWin())
        {
            OnGameOver?.Invoke(true);
            return;
        }
        
        if (Grid.PlayerPerTile.Values.All(value => value != null))
        {
            OnGameOver?.Invoke(false);
            return;
        }
        
        if(GameManager.Instance.CurrentPlayer != GameManager.Instance.PlayerCount) GameManager.Instance.CurrentPlayer++;
        else GameManager.Instance.CurrentPlayer = 1;
        
        StartCoroutine(FadeOutVisual(_image));
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
    
    private bool CheckForWin()
    {
        return false;
        return CheckForHorizontalWin() || CheckForVerticalWin() || CheckForDiagonalWin();
    }
    
    private bool CheckForHorizontalWin()
    {
        for (int row = 0; row < Grid.TileMatrix.GetLength(0); row++)
        {
            if (Grid.PlayerPerTile[Grid.TileMatrix[row, 0]] == null) continue;
            if (Grid.PlayerPerTile[Grid.TileMatrix[row, 0]].GetComponent<Image>().sprite == null) continue;
            if (Grid.PlayerPerTile[Grid.TileMatrix[row, 0]].GetComponent<Image>().sprite != Grid.PlayerPerTile[Grid.TileMatrix[row, 1]].GetComponent<Image>().sprite) continue;
            if (Grid.PlayerPerTile[Grid.TileMatrix[row, 0]].GetComponent<Image>().sprite != Grid.PlayerPerTile[Grid.TileMatrix[row, 2]].GetComponent<Image>().sprite) continue;
            return true;
        }

        return false;
    }
    
    private bool CheckForVerticalWin()
    {
        for (int col = 0; col < Grid.TileMatrix.GetLength(1); col++)
        {
            if (Grid.PlayerPerTile[Grid.TileMatrix[0, col]] == null) continue;
            if (Grid.PlayerPerTile[Grid.TileMatrix[0, col]].GetComponent<Image>().sprite == null) continue;
            if (Grid.PlayerPerTile[Grid.TileMatrix[0, col]].GetComponent<Image>().sprite != Grid.PlayerPerTile[Grid.TileMatrix[1, col]].GetComponent<Image>().sprite) continue;
            if (Grid.PlayerPerTile[Grid.TileMatrix[0, col]].GetComponent<Image>().sprite != Grid.PlayerPerTile[Grid.TileMatrix[2, col]].GetComponent<Image>().sprite) continue;
            return true;
        }

        return false;
    }
    
    private bool CheckForDiagonalWin()
    {
        if (Grid.PlayerPerTile[Grid.TileMatrix[0, 0]] == null) return false;
        if (Grid.PlayerPerTile[Grid.TileMatrix[0, 0]].GetComponent<Image>().sprite == null) return false;
        if (Grid.PlayerPerTile[Grid.TileMatrix[0, 0]].GetComponent<Image>().sprite != Grid.PlayerPerTile[Grid.TileMatrix[1, 1]].GetComponent<Image>().sprite) return false;
        if (Grid.PlayerPerTile[Grid.TileMatrix[0, 0]].GetComponent<Image>().sprite != Grid.PlayerPerTile[Grid.TileMatrix[2, 2]].GetComponent<Image>().sprite) return false;
        return true;
    }
}
