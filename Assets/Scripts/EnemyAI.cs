using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class EnemyAI : TileHandler
{
    [SerializeField] private AIDifficulty aiDifficulty;

    private Dictionary<String, int> _scores;

    private int _currentPlayerToSimulate;
    
    private int _roundToSimulate;
    
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
            case AIDifficulty.MiniMax:
                SmartAI();
                break;
        }
        
        //turn ends and next player is set
        int nextPlayer = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount ? GameManager.Instance.currentPlayer + 1 : 1;
        GameManager.Instance.currentPlayer = nextPlayer;
        PlayerPlaced();
    }

    private void SmartAI()
    {
        int bestScore = int.MinValue;
        Dictionary<String, int> bestMove = new();
        _currentPlayerToSimulate = GameManager.Instance.currentPlayer;
        
        //get all empty tiles
        for(int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            for(int col = 0; col < Grid.Instance.GridWidth; col++)
            {
                //skip if tile is not empty
                if (Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] != null) continue;
                
                //place tile and get score for that new board state
                GameObject AITile = Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] = Instantiate(playerPrefab, Grid.Instance.TileMatrix[row, col].transform.GetChild(0));
                AITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[_currentPlayerToSimulate - 1];
                
                _currentPlayerToSimulate = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount? GameManager.Instance.currentPlayer + 1 : 1;
                _roundToSimulate = GameManager.Instance.round;
                _roundToSimulate++;
                int score = MiniMax(Grid.Instance, 0, false);
                
                //destroy tile and remove reference from grid to reverse changes
                Destroy(Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]]);
                Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] = null;
                
                //update best score and best move
                if(score > bestScore) {
                    bestScore = score;
                    bestMove["row"] = row;
                    bestMove["col"] = col;
                }
            }
        }
        GameObject bestMoveTile = Grid.Instance.TileMatrix[bestMove["row"], bestMove["col"]];
        GameObject enemyAITile = Instantiate(playerPrefab, bestMoveTile.transform.GetChild(0));
        enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
        Grid.Instance.PlayerPerTile[bestMoveTile] = enemyAITile;
    }

    private int MiniMax(Grid board, int depth, bool isMaximizing)
    {
        
        int winner = board.CheckForWin();
        
        if (winner == 0 && _roundToSimulate == board.GridWidth * board.GridWidth) return 0;
        if (winner != 0)
        {
            int score = winner == (_currentPlayerToSimulate = _currentPlayerToSimulate == 1
                ? GameManager.Instance.PlayerCount
                : _currentPlayerToSimulate - 1)
                ? 1
                : -1;
            Debug.Log(score);
            return score;
        }
            
        
        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int row = 0; row < board.GridWidth; row++)
            {
                for (int col = 0; col < board.GridWidth; col++)
                {
                    //skip if tile is not empty
                    if (board.PlayerPerTile[board.TileMatrix[row, col]] != null) continue;
                    
                    //place tile and get score for that new board state
                    
                    GameObject AITile = board.PlayerPerTile[board.TileMatrix[row, col]] = Instantiate(playerPrefab, board.TileMatrix[row, col].transform.GetChild(0));
                    AITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[_currentPlayerToSimulate - 1];
                    
                    _currentPlayerToSimulate = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount? GameManager.Instance.currentPlayer + 1 : 1;
                    _roundToSimulate = _roundToSimulate < board.GridWidth * board.GridWidth ? _roundToSimulate + 1 : GameManager.Instance.round + 1;
                    int score = MiniMax(board, depth + 1, false);
                    
                    //destroy tile and remove reference from grid to reverse changes
                    Destroy(AITile);
                    board.PlayerPerTile[board.TileMatrix[row, col]] = null;
                    
                    //update best score
                    bestScore = Math.Max(score, bestScore);
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            for (int row = 0; row < board.GridWidth; row++)
            {
                for (int col = 0; col < board.GridWidth; col++)
                {
                    //skip if tile is not empty
                    if (board.PlayerPerTile[board.TileMatrix[row, col]] != null) continue;
                    
                    //place tile and get score for that new board state
                    GameObject AITile = board.PlayerPerTile[board.TileMatrix[row, col]] = Instantiate(playerPrefab, board.TileMatrix[row, col].transform.GetChild(0));
                    AITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[_currentPlayerToSimulate - 1];
                    
                    _currentPlayerToSimulate = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount? GameManager.Instance.currentPlayer + 1 : 1;
                    _roundToSimulate = _roundToSimulate < board.GridWidth * board.GridWidth ? _roundToSimulate + 1 : GameManager.Instance.round + 1;
                    int score = MiniMax(board, depth + 1, true);
                    
                    //destroy tile and remove reference from grid to reverse changes
                    Destroy(board.PlayerPerTile[board.TileMatrix[row, col]]);
                    board.PlayerPerTile[board.TileMatrix[row, col]] = null;
                    
                    //update best score
                    bestScore = Math.Min(score, bestScore);
                }
            }
            return bestScore;
        } 
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
        GameObject enemyAITile = Instantiate(playerPrefab, emptyTile.transform.GetChild(0));
        enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
        Grid.Instance.PlayerPerTile[emptyTile] = enemyAITile;
    }

}

public enum AIDifficulty
{
    Dumb,
    MiniMax
}
