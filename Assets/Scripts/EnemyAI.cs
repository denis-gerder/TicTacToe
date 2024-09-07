using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using System.Reflection;
using System.Threading.Tasks;

public class EnemyAI : TileHandler
{
    [SerializeField] private AIDifficulty aiDifficulty;
    private AIDifficulty currentDifficulty;
        

    protected void Awake()
    {
        Grid.OnTurnEnd += HandleTurnEnd;
        Grid.OnGameOver += HandleGameOver;
        currentDifficulty = aiDifficulty;
    }

    private void HandleTurnEnd()
    {
        //return if AI is disabled or if it's not the AI's turn
        if (!GameManager.Instance.IsAIEnabled || GameManager.Instance.currentPlayer == 1 || _gameOver) return;

        if (currentDifficulty == AIDifficulty.Random)
        {
            List<AIDifficulty> allDifficulties = new List<AIDifficulty>();
            allDifficulties.AddRange(Enum.GetValues(typeof(AIDifficulty)).OfType<AIDifficulty>());
            allDifficulties.Remove(AIDifficulty.Random);
            currentDifficulty = allDifficulties[Random.Range(0, allDifficulties.Count)];
        }

        //ai places tile
        switch (currentDifficulty)
        {
            case AIDifficulty.Dumb:
                DumbAI();
                break;
            case AIDifficulty.Optimal:
                SmartAI();
                break;
            case AIDifficulty.OptimalWithRandomness:
                SmartAI();
                break;
        }

        //turn ends and next player is set
        int nextPlayer = GameManager.Instance.currentPlayer != GameManager.Instance.PlayerCount
            ? GameManager.Instance.currentPlayer + 1
            : 1;
        GameManager.Instance.currentPlayer = nextPlayer;
        PlayerPlaced();
    }

    struct TileScore
    {
        public int row;
        public int col;
        public int score;
        public BoardState board;

        public TileScore(int row, int col, int score, BoardState board
        )
        {
            this.row = row;
            this.col = col;
            this.score = score;
            this.board = board;
        }
    }

    struct AsyncProps
    {
        public int row;
        public int col;
        public BoardState boardStateCopy;
        public List<TileScore> tileScores;

        public AsyncProps(int row, int col, BoardState boardStateCopy, List<TileScore> tileScores)
        {
            this.row = row;
            this.col = col;
            this.boardStateCopy = boardStateCopy;
            this.tileScores = tileScores;
        }
    }

    private void SmartAI()
    {
        //store best move to place next tile as row and col
        Dictionary<string, int> bestMove = new();
        //create simple copy of the board state as integers to reduce complexity of placing and removing tiles as game objects
        BoardState originalBoardState = new(Grid.Instance.TileMatrix, Grid.Instance.PlayerPerTile);

        List<TileScore> tileScores = new();

        List<Tuple<int, int, int>> possibleMoves = new();

        for (int row = 0; row < Grid.Instance.GridWidth; row++)
        {
            for(int col = 0; col < Grid.Instance.GridWidth; col++)
            {
                //skip if tile is not empty
                if (Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] != null) 
                    continue;

                possibleMoves.Add(new Tuple<int, int, int>(row, col, 0));
            }
        }

        var tracker = Stopwatch.StartNew();

        foreach (var move in possibleMoves)
        {
            int row = move.Item1;
            int col = move.Item2;

            ThreadPool.QueueUserWorkItem((asyncProps =>
            {
                //set current player and round for copied board state to simulate next move
                ((AsyncProps)asyncProps).boardStateCopy.CurrentPlayer = GameManager.Instance.currentPlayer;
                ((AsyncProps)asyncProps).boardStateCopy.CurrentRound = GameManager.Instance.round + 1;

                //place tile at current test position in copied board
                ((AsyncProps)asyncProps).boardStateCopy.Board[((AsyncProps)asyncProps).row, ((AsyncProps)asyncProps).col] = ((AsyncProps)asyncProps).boardStateCopy.CurrentPlayer;

                BoardTree currentNode = ((AsyncProps)asyncProps).boardStateCopy.currentNode;
                if (GameManager.Instance.EnableLogging)
                    ((AsyncProps)asyncProps).boardStateCopy.AddStateToTree();

                
                //get score for that new board state
                int score = MiniMax(((AsyncProps)asyncProps).boardStateCopy, 0, false,
                    ((AsyncProps)asyncProps).boardStateCopy.CurrentPlayer,
                    ((AsyncProps)asyncProps).boardStateCopy.CurrentRound); 

                ((AsyncProps)asyncProps).boardStateCopy.currentNode.score = score;
                ((AsyncProps)asyncProps).boardStateCopy.currentNode = currentNode;

                //remove reference from copied board to reverse changes
                ((AsyncProps)asyncProps).boardStateCopy.Board[((AsyncProps)asyncProps).row, ((AsyncProps)asyncProps).col] = 0;

                lock (((AsyncProps)asyncProps).tileScores)
                    ((AsyncProps)asyncProps).tileScores.Add(new TileScore(((AsyncProps)asyncProps).row, ((AsyncProps)asyncProps).col, score, ((AsyncProps)asyncProps).boardStateCopy));
            }), new AsyncProps(row, col, new BoardState(originalBoardState), tileScores));
        }

        bool working = true;
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
        while (working)
        {
            ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
            if (workerThreads == maxWorkerThreads)
                working = false; 
        }

        tracker.Stop();
        Debug.Log(tracker.ElapsedMilliseconds);

        TileScore? bestTile = null;
        foreach (var tileScore in tileScores)
        {
            if (bestTile == null || tileScore.score > bestTile.Value.score)
                bestTile = tileScore;
        }

        if (aiDifficulty == AIDifficulty.OptimalWithRandomness)
        {
            tileScores.Remove(bestTile.Value);
            TileScore randomNonOptimal = tileScores[Random.Range(0, tileScores.Count)];
            if (Random.Range(1, 11) <= 3)
            {
                bestTile = randomNonOptimal;
            }
        }

        bestMove["row"] = bestTile.Value.row;
        bestMove["col"] = bestTile.Value.col;

        if (GameManager.Instance.EnableLogging)
        {
            tileScores.ForEach((tilescore) => tilescore.board.PrintTree());
        }
        GameObject bestMoveTile = Grid.Instance.TileMatrix[bestMove["row"], bestMove["col"]];
        GameObject enemyAITile = Instantiate(playerPrefab, bestMoveTile.transform.GetChild(0));
        enemyAITile.GetComponent<Image>().sprite = playerConfigSO.PlayerSymbols[GameManager.Instance.currentPlayer - 1];
        Grid.Instance.PlayerPerTile[bestMoveTile] = enemyAITile;
    }

    private int MiniMax(BoardState board, int depth, bool isMaximizing, int currentPlayer, int currentRound)
    {
        int winner = board.CheckForWin();

        if (winner == 0 && board.CurrentRound == board.Board.GetLength(0) * board.Board.GetLength(1))
            return 0;
        
        if (winner != 0)
        {
            int score = winner == GameManager.Instance.currentPlayer ? 1 : -1;
            return score;
        }

        List<Tuple<int, int, int>> possibleMoves = new();

        if (isMaximizing)
        {
            //bestScore is initially the worst possible score, so its updated only when its better
            int bestScore = -1;
            int bestPossibleScore = 1;

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

                    int possibleTerminalScore = board.CheckForWin();
                    if (possibleTerminalScore != 0)
                        possibleTerminalScore = possibleTerminalScore == GameManager.Instance.currentPlayer ? 1 : -1;

                    board.Board[row, col] = 0;

                    possibleMoves.Add(new Tuple<int, int, int>(row, col, possibleTerminalScore));
                }
            }

            possibleMoves.Sort((x , y) => y.Item3.CompareTo(x.Item3));

            foreach (var move in possibleMoves)
            {
                board.CurrentPlayer = currentPlayer != GameManager.Instance.PlayerCount ? currentPlayer + 1 : 1;
                board.CurrentRound = currentRound + 1;
                //place tile and get score for that new board state
                board.Board[move.Item1, move.Item2] = board.CurrentPlayer;

                BoardTree currentNode = board.currentNode;
                if (GameManager.Instance.EnableLogging)
                    board.AddStateToTree();

                int score = MiniMax(board, depth + 1, false, board.CurrentPlayer, board.CurrentRound);
                board.currentNode.score = score;
                board.currentNode = currentNode;

                //destroy tile and remove reference from grid to reverse changes
                board.Board[move.Item1, move.Item2] = 0;

                //update best score
                bestScore = Math.Max(score, bestScore);
                if (score ==  bestPossibleScore)
                {
                    return score;
                }
            }

            return bestScore;
        }
        else
        {
            //bestScore is initially the worst possible score, so its updated only when its better
            int bestScore = 1;
            int bestPossibleScore = -1;

            for (int row = 0; row < board.Board.GetLength(0); row++)
            {
                for (int col = 0; col < board.Board.GetLength(0); col++)
                {
                    //skip if tile is not empty
                    if (board.Board[row, col] != 0) continue;

                    board.CurrentPlayer = currentPlayer != GameManager.Instance.PlayerCount ? currentPlayer + 1 : 1;
                    board.CurrentRound = currentRound + 1;
                    //place tile and get score for that new board state
                    board.Board[row, col] = board.CurrentPlayer;

                    int possibleTerminalScore = board.CheckForWin();
                    if (possibleTerminalScore != 0)
                        possibleTerminalScore = possibleTerminalScore == GameManager.Instance.currentPlayer ? 1 : -1;

                    board.Board[row, col] = 0;

                    possibleMoves.Add(new Tuple<int, int, int>(row, col, possibleTerminalScore));
                }
            }

            possibleMoves.Sort((x, y) => x.Item3.CompareTo(y.Item3));

            foreach (var move in possibleMoves)
            {
                board.CurrentPlayer = currentPlayer != GameManager.Instance.PlayerCount ? currentPlayer + 1 : 1;
                board.CurrentRound = currentRound + 1;
                //place tile and get score for that new board state
                board.Board[move.Item1, move.Item2] = board.CurrentPlayer;

                BoardTree currentNode = board.currentNode;
                if (GameManager.Instance.EnableLogging)
                    board.AddStateToTree();

                int score = MiniMax(board, depth + 1, true, board.CurrentPlayer, board.CurrentRound);
                board.currentNode.score = score;
                board.currentNode = currentNode;

                //destroy tile and remove reference from grid to reverse changes
                board.Board[move.Item1, move.Item2] = 0;

                bestScore = Math.Min(score, bestScore);
                if (score == bestPossibleScore)
                    return score;
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
                if(Grid.Instance.PlayerPerTile[Grid.Instance.TileMatrix[row, col]] == null) 
                    emptyTiles.Add(Grid.Instance.TileMatrix[row, col]);
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

    private readonly BoardTree root;

    public BoardTree currentNode;


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
        root = new BoardTree();
        currentNode = root;
    } 

    public BoardState(BoardState boardState)
    {
        Board = (int[,])boardState.Board.Clone();
        CurrentRound = boardState.CurrentRound;
        CurrentPlayer = boardState.CurrentPlayer;
        root = new BoardTree();
        currentNode = root;
    }

    public void AddStateToTree()
    {
        currentNode = currentNode.AddChild(CopyBoardState());
    }

    public int[,] CopyBoardState() => (int[,]) Board.Clone();

    public void PrintTree()
    {
        Debug.Log(root.PrintTree());
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
}

public enum AIDifficulty
{
    Random,
    Dumb,
    Optimal,
    OptimalWithRandomness
}
