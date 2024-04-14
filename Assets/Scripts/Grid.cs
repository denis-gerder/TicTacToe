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
                rectTransform.anchoredPosition = new Vector3(tileScale * col * tileWidth, tileScale * row * tileWidth, 0);
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
        int winner = CheckForWin();
        if(winner != 0) OnGameOver?.Invoke(true, winner);
        
        GameManager.Instance.round++;
        if(GameManager.Instance.round == _gridWidth * _gridWidth && winner == 0) OnGameOver?.Invoke(false, player);
        
        //end turn
        OnTurnEnd?.Invoke();
    }
    
    public int CheckForWin()
    {
        int horizontalWin = CheckForHorizontalWin();
        int verticalWin = CheckForVerticalWin();
        int diagonalWin = CheckForDiagonalWin();
        if(horizontalWin != 0) return horizontalWin;
        if(verticalWin != 0) return verticalWin;
        if(diagonalWin != 0) return diagonalWin;
        return 0;
    }
    
    private int CheckForHorizontalWin()
    {
        for (int row = 0; row < Instance._gridWidth; row++)
        {
            if(Instance.PlayerPerTile[Instance.TileMatrix[row, 0]] == null) continue;
            Sprite playerSymbol = Instance.PlayerPerTile[Instance.TileMatrix[row, 0]].GetComponent<Image>().sprite;
            
            
            int playerTilesInRow = 1;
            for (int col = 1; col < Instance._gridWidth; col++)
            {
                if(Instance.PlayerPerTile[Instance.TileMatrix[row, col]] == null) break;
                if(playerSymbol != Instance.PlayerPerTile[Instance.TileMatrix[row, col]].GetComponent<Image>().sprite) break;
                playerTilesInRow++;
            }
            if (playerTilesInRow == Instance._gridWidth)
            {
                int player = Instance.PlayerPerTile[Instance.TileMatrix[row, 0]].transform.parent.GetComponent<TileHandler>().playerConfigSO.PlayerSymbols.IndexOf(playerSymbol)+1;
                return player;
            }
        }
        return 0;
    }
    
    private int CheckForVerticalWin()
    {
        for (int col = 0; col < Instance._gridWidth; col++)
        {
            if(Instance.PlayerPerTile[Instance.TileMatrix[0, col]] == null) continue;
            Sprite playerSymbol = Instance.PlayerPerTile[Instance.TileMatrix[0, col]].GetComponent<Image>().sprite;
            
            int playerTilesInCol = 1;
            for (int row = 1; row < Instance._gridWidth; row++)
            {
                if(Instance.PlayerPerTile[Instance.TileMatrix[row, col]] == null) break;
                if(playerSymbol != Instance.PlayerPerTile[Instance.TileMatrix[row, col]].GetComponent<Image>().sprite) break;
                playerTilesInCol++;
            }

            if (playerTilesInCol == Instance._gridWidth)
            {
                int player = Instance.PlayerPerTile[Instance.TileMatrix[0, col]].transform.parent.GetComponent<TileHandler>().playerConfigSO.PlayerSymbols.IndexOf(playerSymbol)+1;
                return player;
            }
        }
        return 0;
    }
    
    private int CheckForDiagonalWin()
    {
        int playerTilesInDgl1 = 0;
        int player = 0;
        if (Instance.PlayerPerTile[Instance.TileMatrix[0, 0]] != null)
        {
            Sprite playerSymbol1 = Instance.PlayerPerTile[Instance.TileMatrix[0, 0]].GetComponent<Image>().sprite;

            playerTilesInDgl1 = 1;

            for (int row = 1; row < Instance._gridWidth; row++)
            {
                if (Instance.PlayerPerTile[Instance.TileMatrix[row, row]] == null) break;
                if (playerSymbol1 != Instance.PlayerPerTile[Instance.TileMatrix[row, row]].GetComponent<Image>().sprite) break;
                playerTilesInDgl1++;
            }
            
            if(playerTilesInDgl1 == Instance._gridWidth)
                player = Instance.PlayerPerTile[Instance.TileMatrix[0, 0]].transform.parent.GetComponent<TileHandler>().playerConfigSO.PlayerSymbols.IndexOf(playerSymbol1)+1;
        }
        
        int playerTilesInDgl2 = 0;
        if (Instance.PlayerPerTile[Instance.TileMatrix[0, Instance._gridWidth - 1]] != null)
        {
            Sprite playerSymbol2 = Instance.PlayerPerTile[Instance.TileMatrix[0, Instance._gridWidth - 1]].GetComponent<Image>().sprite;
                        
            playerTilesInDgl2 = 1;
            
            for (int row = 1; row < Instance._gridWidth; row++)
            {

                if(Instance.PlayerPerTile[Instance.TileMatrix[row, Instance._gridWidth - row - 1]] == null) break;
                if(playerSymbol2 != Instance.PlayerPerTile[Instance.TileMatrix[row, Instance._gridWidth - row - 1]].GetComponent<Image>().sprite) break;
                playerTilesInDgl2++;
            }
            
            if(playerTilesInDgl2 == Instance._gridWidth)
                player = Instance.PlayerPerTile[Instance.TileMatrix[0, Instance._gridWidth - 1]].transform.parent.GetComponent<TileHandler>().playerConfigSO.PlayerSymbols.IndexOf(playerSymbol2)+1;
        }
        if(playerTilesInDgl1 == Instance._gridWidth || playerTilesInDgl2 == Instance._gridWidth) return player;
        
        return player;
    }
}
