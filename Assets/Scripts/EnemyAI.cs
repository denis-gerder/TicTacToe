using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAI : TileHandler
{
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
        DumbAI();
        
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
        enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1].Sprite;
        enemyAITile.GetComponent<Image>().color = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1].Color;
        Grid.Instance.PlayerPerTile[emptyTile] = enemyAITile;
    }
}
