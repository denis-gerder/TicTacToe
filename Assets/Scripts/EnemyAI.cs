using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAI : TileHandler
{
    [SerializeField] private AIDifficulty aiDifficulty;
    
    protected void Awake()
    {
        Grid.OnTurnEnd += HandleTurnEnd;
        Grid.OnGameOver += HandleGameOver;
    }
    
    private void HandleTurnEnd()
    {
        //return if AI is disabled or if it's not the AI's turn
        if (!GameManager.Instance.IsAIEnabled || GameManager.Instance.currentPlayer == 1 || _gameOver) return;
        
        //ai places tile
        switch (aiDifficulty)
        {
            case AIDifficulty.Dumb:
                DumbAI();
                break;
            case AIDifficulty.Smart: 
                SmartAI();
                break;
        }
        
        //turn ends and next player is set
        int nextPlayer = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount ? GameManager.Instance.currentPlayer + 1 : 1;
        GameManager.Instance.currentPlayer = nextPlayer;
        PlayerPlaced();
    }

    private void DumbAI()
    {
        //get all empty tiles
        List<GameObject> emptyTiles = new();
        for(int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            for(int col = 0; col < Grid.Instance.GridWidth; col++)
            {
                if(Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] == null) emptyTiles.Add(Grid.Instance.TileMatrix[row, col]);
            }
        }
        //get random empty tile and place player tile with reference in grid
        GameObject emptyTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
        GameObject enemyAITile = Instantiate(playerPrefab, emptyTile.transform);
        enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
        Grid.Instance.PlayerPerTile[emptyTile] = enemyAITile;
    }

    private void SmartAI()
    {
        //place tile to prevent player win if possible
        bool playerWinPrevented = PreventPlayerWin();
        if(playerWinPrevented) return;
        
        //place tile to win if possible
        PlaceSmartTile();
    }

    private bool PreventPlayerWin()
    {
        bool playerWinPrevented = false;

        playerWinPrevented = CheckForCloseHorizontalPlayerWin() || CheckForCloseVericalPlayerWin() || CheckForCloseDiagonalWin();
        
        return playerWinPrevented;
    }

    private void PlaceSmartTile()
    {
        
    }
    
    private bool CheckForCloseVericalPlayerWin()
    {
        Vector2Int emptyTilePosition = new Vector2Int(0, 0);
        for (int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            int playerTilesInCol = 0;
            
            for (int col = 0; col < Grid.Instance.GridWidth; col++)
            {
                if(Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] == null) emptyTilePosition = new Vector2Int(row, col);
                
                //check if tile is placed by player and not empty
                else if(playerConfigSO.PlayerSymbols[0] == Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]].GetComponent<Image>().sprite) playerTilesInCol++;
                
                
            }
            if (playerTilesInCol == Grid.Instance.GridWidth - 1)
            {
                
                GameObject enemyAITile = Instantiate(playerPrefab, Grid.Instance.TileMatrix[emptyTilePosition.x, emptyTilePosition.y].transform);
                enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
                Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[emptyTilePosition.x, emptyTilePosition.y]] = enemyAITile;
                return true;
            }
        }
        return false;
    }
    
    private bool CheckForCloseHorizontalPlayerWin()
    {
        Vector2Int emptyTilePosition = new Vector2Int(0, 0);
        for (int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            int playerTilesInRow = 0;
            
            for (int col = 0; col < Grid.Instance.GridWidth; col++)
            {
                if(Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[col, row]] == null) emptyTilePosition = new Vector2Int(col, row);
                
                //check if tile is placed by player and not empty
                else if(playerConfigSO.PlayerSymbols[0] == Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[col, row]].GetComponent<Image>().sprite) playerTilesInRow++;
                
                
            }
            if (playerTilesInRow == Grid.Instance.GridWidth - 1)
            {
                GameObject enemyAITile = Instantiate(playerPrefab, Grid.Instance.TileMatrix[emptyTilePosition.x, emptyTilePosition.y].transform);
                enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
                Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[emptyTilePosition.x, emptyTilePosition.y]] = enemyAITile;
                return true;
            }
        }
        return false;
    }
    
    private bool CheckForCloseDiagonalWin()
    {
        Vector2Int emptyTilePosition1 = new Vector2Int(0, 0);
        int playerTilesInDgl1 = 0;

        for (int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            if (Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, row]] == null) emptyTilePosition1 = new Vector2Int(row, row);
            else if (playerConfigSO.PlayerSymbols[0] == Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, row]].GetComponent<Image>().sprite) playerTilesInDgl1++;
        }
        
        if (playerTilesInDgl1 == Grid.Instance.GridWidth - 1)
        {
            GameObject enemyAITile = Instantiate(playerPrefab, Grid.Instance.TileMatrix[emptyTilePosition1.x, emptyTilePosition1.y].transform);
            enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
            Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[emptyTilePosition1.x, emptyTilePosition1.y]] = enemyAITile;
            return true;
        }
        
        Vector2Int emptyTilePosition2 = new Vector2Int(0, 0);
        int playerTilesInDgl2 = 0;
        for (int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            if (Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, Grid.Instance.GridWidth - row - 1]] == null) emptyTilePosition2 = new Vector2Int(row, Grid.Instance.GridWidth - row - 1);
            else if (playerConfigSO.PlayerSymbols[0] == Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, Grid.Instance.GridWidth - row - 1]].GetComponent<Image>().sprite) playerTilesInDgl2++;
        }
        
        if (playerTilesInDgl2 == Grid.Instance.GridWidth - 1)
        {
            GameObject enemyAITile = Instantiate(playerPrefab, Grid.Instance.TileMatrix[emptyTilePosition2.x, emptyTilePosition2.y].transform);
            enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
            Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[emptyTilePosition2.x, emptyTilePosition2.y]] = enemyAITile;
            return true;
        }
        
        return false;
    }
     
}

public enum AIDifficulty
{
    Dumb,
    Smart
}
