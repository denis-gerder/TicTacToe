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
        //store best move to place next tile as row and col
        Dictionary<String, int> bestMove = new();
        //create simple copy of the board state as integers to reduce complexity of placing and removing tiles as game objects
        BoardState boardStateCopy = new(Grid.Instance.TileMatrix, Grid.Instance.PlayerPerTile);
        
        for(int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            for(int col = 0; col < Grid.Instance.GridWidth; col++)
            {
                //skip if tile is not empty
                if (Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] != null) continue;
                
                //set current player and round for copied board state to simulate next move
                boardStateCopy.CurrentPlayer = GameManager.Instance.currentPlayer;
                boardStateCopy.CurrentRound = GameManager.Instance.round+1;
                
                //place tile at current test position in copied board
                boardStateCopy.Board[row, col] = boardStateCopy.CurrentPlayer;

                if(GameManager.Instance.EnableLogging)
                    boardStateCopy.AddStateToDebugString();

                //get score for that new board state
                int score = MiniMax(boardStateCopy, 0, false, boardStateCopy.CurrentPlayer, boardStateCopy.CurrentRound, int.MinValue, int.MaxValue);
                
                //remove reference from copied board to reverse changes
                boardStateCopy.Board[row, col] = 0;
                
                //update best score and best move
                if(score > bestScore) {
                    bestScore = score;
                    bestMove["row"] = row;
                    bestMove["col"] = col;
                }
                if (GameManager.Instance.EnableLogging)
                {
                    boardStateCopy.placeSequenceString += " Best Score: " + bestScore + "\n";
                    Debug.Log(boardStateCopy.placeSequenceString);
                    boardStateCopy.placeSequenceString = "";
                }
            }
        }
        GameObject bestMoveTile = Grid.Instance.TileMatrix[bestMove["row"], bestMove["col"]];
        GameObject enemyAITile = Instantiate(playerPrefab, bestMoveTile.transform.GetChild(0));
        enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
        Grid.Instance.PlayerPerTile[bestMoveTile] = enemyAITile;
    }

    private int MiniMax(BoardState board, int depth, bool isMaximizing, int currentPlayer, int currentRound, int alpha, int beta)
    {
        int winner = board.CheckForWin();

        if (winner == 0 && board.CurrentRound == board.Board.GetLength(0) * board.Board.GetLength(1))
        {
            if (GameManager.Instance.EnableLogging)
                board.AddStateToDebugString(0);
            return 0;
        }
        if (winner != 0)
        {
            int score = winner == GameManager.Instance.currentPlayer ? 1 : -1;
            if (GameManager.Instance.EnableLogging)
                board.AddStateToDebugString(score);
            return score;
        }
        
        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int row = 0; row < board.Board.GetLength(0); row++)
            {
                for (int col = 0; col < board.Board.GetLength(0); col++)
                {
                    //skip if tile is not empty
                    if (board.Board[row, col] != 0) continue;
                    
                    board.CurrentPlayer = currentPlayer != GameManager.Instance.PlayerCount? currentPlayer + 1 : 1;
                    board.CurrentRound = currentRound + 1;
                    //place tile and get score for that new board state
                    board.Board[row, col] = board.CurrentPlayer;

                    if (GameManager.Instance.EnableLogging)
                        board.AddStateToDebugString();

                    int score = MiniMax(board, depth + 1, false, board.CurrentPlayer, board.CurrentRound, int.MinValue, int.MaxValue);
                    
                    //destroy tile and remove reference from grid to reverse changes
                    board.Board[row, col] = 0;
                    
                    //update best score
                    bestScore = Math.Max(score, bestScore);
                    alpha = Math.Max(alpha, bestScore);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            for (int row = 0; row < board.Board.GetLength(0); row++)
            {
                for (int col = 0; col < board.Board.GetLength(0); col++)
                {
                    //skip if tile is not empty
                    if (board.Board[row, col] != 0) continue;
                    
                    board.CurrentPlayer = currentPlayer != GameManager.Instance.PlayerCount? currentPlayer + 1 : 1;
                    board.CurrentRound = currentRound + 1;
                    //place tile and get score for that new board state
                    board.Board[row, col] = board.CurrentPlayer;

                    if (GameManager.Instance.EnableLogging)
                        board.AddStateToDebugString();

                    int score = MiniMax(board, depth + 1, true, board.CurrentPlayer, board.CurrentRound, int.MinValue, int.MaxValue);
                    
                    //destroy tile and remove reference from grid to reverse changes
                    board.Board[row, col] = 0;
                    
                    //update best score
                    bestScore = Math.Min(score, bestScore);
                    beta = Math.Min(beta, bestScore);
                    if (beta <= alpha)
                    {
                        break;
                    }
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

public class BoardState
{
    public int[,] Board;

    public int CurrentRound;
    
    public int CurrentPlayer;

    public String placeSequenceString;
    
    public BoardState(GameObject[,] tileMatrix, Dictionary<GameObject, GameObject> playerPerTile)
    {
        Board = new int[tileMatrix.GetLength(0), tileMatrix.GetLength(1)];
        for (int row = 0; row < tileMatrix.GetLength(0); row++)
        {
            for (int col = 0; col < tileMatrix.GetLength(1); col++)
            {
                Board[row, col] = playerPerTile[tileMatrix[row, col]] == null ? 0 : Int32.Parse(playerPerTile[tileMatrix[row, col]].GetComponent<Image>().sprite.name[6].ToString());
            }
        }
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
        for (int row = 0; row < Board.GetLength(0); row++)
        {
            if(Board[row, 0] == 0) continue;
            int playerSymbol = Board[row, 0];
            
            int playerTilesInRow = 1;
            for (int col = 1; col < Board.GetLength(1); col++)
            {
                if(Board[row, col] == 0) break;
                if(playerSymbol != Board[row, col]) break;
                playerTilesInRow++;
            }
            if (playerTilesInRow == Board.GetLength(1))
            {
                return playerSymbol;
            }
        }
        return 0;
    }
    
    private int CheckForVerticalWin()
    {
        for (int col = 0; col < Board.GetLength(1); col++)
        {
            if(Board[0, col] == 0) continue;
            int playerSymbol = Board[0, col];
            
            int playerTilesInCol = 1;
            for (int row = 1; row < Board.GetLength(0); row++)
            {
                if(Board[row, col] == 0) break;
                if(playerSymbol != Board[row, col]) break;
                playerTilesInCol++;
            }

            if (playerTilesInCol == Board.GetLength(0))
            {
                return playerSymbol;
            }
        }
        return 0;
    }
    
    private int CheckForDiagonalWin()
    {
        if(Board[0, 0] != 0)
        {
            int playerSymbol = Board[0, 0];
            int playerTilesInDiagonal = 1;
            for (int i = 1; i < Board.GetLength(0); i++)
            {
                if(Board[i, i] == 0) break;
                if(playerSymbol != Board[i, i]) break;
                playerTilesInDiagonal++;
            }
            if (playerTilesInDiagonal == Board.GetLength(0))
            {
                return playerSymbol;
            }
        }
        if(Board[0, Board.GetLength(1) - 1] != 0)
        {
            int playerSymbol = Board[0, Board.GetLength(1) - 1];
            int playerTilesInDiagonal = 1;
            for (int i = 1; i < Board.GetLength(0); i++)
            {
                if(Board[i, Board.GetLength(1) - 1 - i] == 0) break;
                if(playerSymbol != Board[i, Board.GetLength(1) - 1 - i]) break;
                playerTilesInDiagonal++;
            }
            if (playerTilesInDiagonal == Board.GetLength(0))
            {
                return playerSymbol;
            }
        }
        return 0;
    }

    public void AddStateToDebugString(int score)
    {

        placeSequenceString += " Score:" + score + "\n------\n";
    }

    public void AddStateToDebugString()
    {
        String boardString = "";
        for (int row = 0; row < Board.GetLength(0); row++)
        {
            String rowString = "";
            for (int col = 0; col < Board.GetLength(1); col++)
            {
                rowString += " " +  Board[row, col] + " ";
            }
            boardString = rowString + "\n" + boardString;
        }

        placeSequenceString += boardString + "------\n";
    }
}

public enum AIDifficulty
{
    Dumb,
    MiniMax
}
