using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class Grid
{
    public static Grid Instance;
    
    //singleton instance of the grid
    public GameObject GridInstance;

    //dictionary to keep track of which player placed a tile on which tile
    public Dictionary<GameObject, GameObject> PlayerPerTile = new();

    //matrix to keep track of the tiles
    public GameObject[,] TileMatrix;
    
    //width of the grid
    private int _gridWidth;

    public int GridWidth => _gridWidth;

    public static event Action<bool, int> OnGameOver;
    
    public static event Action OnTurnEnd;
    
    public Grid(GameObject canvas, GameObject tilePrefab, int gridWidth)
    {
        Instance = this;
        
        _gridWidth = gridWidth;
        
        GridInstance = Object.Instantiate(new GameObject("Grid"), canvas.transform);
        
        TileMatrix = new GameObject[gridWidth, gridWidth];
        
        
        float tileScale = 1f / gridWidth * 8;
        float tileWidth = tilePrefab.GetComponent<RectTransform>().rect.width;
        
        for (int row = 0; row < gridWidth; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                GameObject tileInstance = Object.Instantiate(tilePrefab, GridInstance.transform);
                PlayerPerTile.Add(tileInstance, null);
                TileMatrix[row, col] = tileInstance;
                
                RectTransform rectTransform = tileInstance.GetComponent<RectTransform>();
                rectTransform.localScale *= tileScale;
                rectTransform.anchoredPosition = new Vector3(tileScale * row * tileWidth, tileScale * col * tileWidth, 0);
            }
        }
        
        TileHandler.OnPlayerTilePlaced += HandlePlayerTilePlaced;
        
        CenterGrid(gridWidth, tileWidth, tileScale);
    }

    private void CenterGrid(int gridWidth, float tileWidth, float tileScale)
    {
        GridInstance.transform.localPosition = new Vector3(
            -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 
            -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 
            0);
    }
    
    private void HandlePlayerTilePlaced(int player)
    {
        //check if game is won or if it's a draw
        bool isGameWon = CheckForWin();
        if(isGameWon) OnGameOver?.Invoke(true, player);
        
        GameManager.Instance.round++;
        if(GameManager.Instance.round == _gridWidth * _gridWidth && !isGameWon) OnGameOver?.Invoke(false, player);
        
        //end turn
        OnTurnEnd?.Invoke();
    }
    
    public bool CheckForWin()
    {
        return CheckForHorizontalWin() || CheckForVerticalWin() || CheckForDiagonalWin();
    }
    
    private bool CheckForHorizontalWin()
    {
        for (int row = 0; row < Instance._gridWidth; row++)
        {
            if(Instance.PlayerPerTile[Instance.TileMatrix[row, 0]] == null) continue;
            Color playerColor = Instance.PlayerPerTile[Instance.TileMatrix[row, 0]].GetComponent<Image>().color;
            
            int playerTilesInRow = 1;
            for (int col = 1; col < Instance._gridWidth; col++)
            {
                if(Instance.PlayerPerTile[Instance.TileMatrix[row, col]] == null) break;
                if(playerColor != Instance.PlayerPerTile[Instance.TileMatrix[row, col]].GetComponent<Image>().color) break;
                playerTilesInRow++;
            }
            if(playerTilesInRow == Instance._gridWidth) return true;
        }
        return false;
    }
    
    private bool CheckForVerticalWin()
    {
        for (int col = 0; col < Instance._gridWidth; col++)
        {
            if(Instance.PlayerPerTile[Instance.TileMatrix[0, col]] == null) continue;
            Color playerColor = Instance.PlayerPerTile[Instance.TileMatrix[0, col]].GetComponent<Image>().color;
            
            int playerTilesInCol = 1;
            for (int row = 1; row < Instance._gridWidth; row++)
            {
                if(Instance.PlayerPerTile[Instance.TileMatrix[row, col]] == null) break;
                if(playerColor != Instance.PlayerPerTile[Instance.TileMatrix[row, col]].GetComponent<Image>().color) break;
                playerTilesInCol++;
            }
            if(playerTilesInCol == Instance._gridWidth) return true;
        }
        return false;
    }
    
    private bool CheckForDiagonalWin()
    {
        int playerTilesInDgl1 = 0;
        if (Instance.PlayerPerTile[Instance.TileMatrix[0, 0]] != null)
        {
            Color playerColor1 = Instance.PlayerPerTile[Instance.TileMatrix[0, 0]].GetComponent<Image>().color;

            playerTilesInDgl1 = 1;

            for (int row = 1; row < Instance._gridWidth; row++)
            {
                if (Instance.PlayerPerTile[Instance.TileMatrix[row, row]] == null) break;
                if (playerColor1 != Instance.PlayerPerTile[Instance.TileMatrix[row, row]].GetComponent<Image>().color) break;
                playerTilesInDgl1++;
            }
        }
        
        int playerTilesInDgl2 = 0;
        if (Instance.PlayerPerTile[Instance.TileMatrix[0, Instance._gridWidth - 1]] != null)
        {
            Color playerColor2 = Instance.PlayerPerTile[Instance.TileMatrix[0, Instance._gridWidth - 1]].GetComponent<Image>().color;
                        
            playerTilesInDgl2 = 1;
            
            for (int row = 1; row < Instance._gridWidth; row++)
            {

                if(Instance.PlayerPerTile[Instance.TileMatrix[row, Instance._gridWidth - row - 1]] == null) break;
                if(playerColor2 != Instance.PlayerPerTile[Instance.TileMatrix[row, Instance._gridWidth - row - 1]].GetComponent<Image>().color) break;
                playerTilesInDgl2++;
            }
        }
        if(playerTilesInDgl1 == Instance._gridWidth || playerTilesInDgl2 == Instance._gridWidth) return true;
        
        return false;
    }
}
