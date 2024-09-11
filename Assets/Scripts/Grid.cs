using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class Grid
{
    public int CurrentPlayer { get; private set; } = 1;

    public int Round { get; private set; } = 1;

    public int GridWidth { get; private set; }

    //singleton instance of the grid
    private readonly GameObject _gridInstance;

    //dictionary to keep track of which player placed a tile on which tile
    public Dictionary<GameObject, GameObject> PlayerPerTile = new();

    //matrix to keep track of the tiles
    public GameObject[,] TileMatrix;

    public event Action<bool, int> OnGameOver;
    
    public event Action OnTurnEnd;
    
    public Grid(GameObject canvas, GameObject tilePrefab, int gridWidth)
    {
        GridWidth = gridWidth;
        _gridInstance = Object.Instantiate(new GameObject("Grid"), canvas.transform);
        TileMatrix = new GameObject[gridWidth, gridWidth];
        
        float tileScale = 1f / gridWidth * 8;
        float tileWidth = tilePrefab.GetComponent<RectTransform>().rect.width;
        
        for (int row = 0; row < gridWidth; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                GameObject tileInstance = Object.Instantiate(tilePrefab, _gridInstance.transform);
                TileHandler tileHandlerInstance = tileInstance.GetComponentInChildren<TileHandler>();
                tileHandlerInstance.SetupPlayingFieldReference(this);

                PlayerPerTile.Add(tileInstance, null);
                TileMatrix[row, col] = tileInstance;
                
                RectTransform rectTransform = tileInstance.GetComponent<RectTransform>();
                rectTransform.localScale *= tileScale;
                rectTransform.anchoredPosition = new Vector3(tileScale * col * tileWidth, tileScale * row * tileWidth, 0);
            }
        }
        
        CenterGrid(gridWidth, tileWidth, tileScale);

        TileHandler.OnPlayerTilePlaced += HandlePlayerTilePlaced;
        _gridInstance.AddComponent<EnemyAI>().SetupPlayingFieldReference(this);
    }

    private void CenterGrid(int gridWidth, float tileWidth, float tileScale)
    {
        _gridInstance.transform.localPosition = new Vector3(
            -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 
            -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 
            0);
    }
    
    private void HandlePlayerTilePlaced()
    {
        //check if game is won or if it's a draw
        int winner = CheckForWin();
        if(winner != 0) OnGameOver?.Invoke(true, winner);
        
        if(Round == GridWidth * GridWidth && winner == 0) 
            OnGameOver?.Invoke(false, CurrentPlayer);

        Round++;
        CurrentPlayer = CurrentPlayer != GameManager.Instance.PlayerCount ? CurrentPlayer + 1 : 1;

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
        for (int row = 0; row < GridWidth; row++)
        {
            if(PlayerPerTile[TileMatrix[row, 0]] == null) 
                continue;
            Sprite playerSymbol = PlayerPerTile[TileMatrix[row, 0]].GetComponent<Image>().sprite;
            
            
            int playerTilesInRow = 1;
            for (int col = 1; col < GridWidth; col++)
            {
                if(PlayerPerTile[TileMatrix[row, col]] == null) 
                    break;
                if(playerSymbol != PlayerPerTile[TileMatrix[row, col]].GetComponent<Image>().sprite) 
                    break;
                playerTilesInRow++;
            }
            if (playerTilesInRow == GridWidth)
            {
                int player = PlayerPerTile[TileMatrix[row, 0]].transform.parent.GetComponent<TileHandler>().PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol)+1;
                return player;
            }
        }
        return 0;
    }
    
    private int CheckForVerticalWin()
    {
        for (int col = 0; col < GridWidth; col++)
        {
            if(PlayerPerTile[TileMatrix[0, col]] == null) 
                continue;
            Sprite playerSymbol = PlayerPerTile[TileMatrix[0, col]].GetComponent<Image>().sprite;
            
            int playerTilesInCol = 1;
            for (int row = 1; row < GridWidth; row++)
            {
                if(PlayerPerTile[TileMatrix[row, col]] == null) 
                    break;
                if(playerSymbol != PlayerPerTile[TileMatrix[row, col]].GetComponent<Image>().sprite) 
                    break;
                playerTilesInCol++;
            }

            if (playerTilesInCol == GridWidth)
            {
                int player = PlayerPerTile[TileMatrix[0, col]].transform.parent.GetComponent<TileHandler>().PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol)+1;
                return player;
            }
        }
        return 0;
    }
    
    private int CheckForDiagonalWin()
    {
        int playerTilesInDgl1 = 0;
        int player = 0;
        if (PlayerPerTile[TileMatrix[0, 0]] != null)
        {
            Sprite playerSymbol1 = PlayerPerTile[TileMatrix[0, 0]].GetComponent<Image>().sprite;

            playerTilesInDgl1 = 1;

            for (int row = 1; row < GridWidth; row++)
            {
                if (PlayerPerTile[TileMatrix[row, row]] == null) 
                    break;
                if (playerSymbol1 != PlayerPerTile[TileMatrix[row, row]].GetComponent<Image>().sprite) 
                    break;
                playerTilesInDgl1++;
            }
            
            if(playerTilesInDgl1 == GridWidth)
                player = PlayerPerTile[TileMatrix[0, 0]].transform.parent.GetComponent<TileHandler>().PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol1)+1;
        }
        
        int playerTilesInDgl2 = 0;
        if (PlayerPerTile[TileMatrix[0, GridWidth - 1]] != null)
        {
            Sprite playerSymbol2 = PlayerPerTile[TileMatrix[0, GridWidth - 1]].GetComponent<Image>().sprite;
                        
            playerTilesInDgl2 = 1;
            
            for (int row = 1; row < GridWidth; row++)
            {

                if(PlayerPerTile[TileMatrix[row, GridWidth - row - 1]] == null) 
                    break;
                if(playerSymbol2 != PlayerPerTile[TileMatrix[row, GridWidth - row - 1]].GetComponent<Image>().sprite) 
                    break;
                playerTilesInDgl2++;
            }
            
            if(playerTilesInDgl2 == GridWidth)
                player = PlayerPerTile[TileMatrix[0, GridWidth - 1]].transform.parent.GetComponent<TileHandler>().PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol2)+1;
        }
        if(playerTilesInDgl1 == GridWidth || playerTilesInDgl2 == GridWidth) return player;
        
        return player;
    }
}
