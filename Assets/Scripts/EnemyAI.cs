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
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine.Tilemaps;

public enum AIDifficulty
{
    Random,
    Dumb,
    Optimal,
    OptimalWithRandomness
}


public class EnemyAI : MonoBehaviour
{
    private AIDifficulty _currentDifficulty;
    private int _maxDepth;
    private long _durationOfAlgorithm;
    private int _configuredMaxDepth = 7;
    private Grid _playingField;

    protected void Awake()
    {
        _currentDifficulty = GameManager.Instance.AiDifficulty;
    }

    public void SetupPlayingFieldReference(Grid playingField)
    {
        _playingField = playingField;
        _playingField.OnTurnEnd += HandleTurnEnd;
    }

    private void HandleTurnEnd()
    {
        //return if AI is disabled or if it's not the AI's turn
        if (!GameManager.Instance.IsAiEnabled || _playingField.CurrentPlayer == 1 || GameManager.Instance.GameOver) 
            return;

        if (_currentDifficulty == AIDifficulty.Random)
        {
            List<AIDifficulty> allDifficulties = new();
            allDifficulties.AddRange(Enum.GetValues(typeof(AIDifficulty)).OfType<AIDifficulty>());
            allDifficulties.Remove(AIDifficulty.Random);
            _currentDifficulty = allDifficulties[Random.Range(0, allDifficulties.Count)];
        }

        //ai places tile
        switch (_currentDifficulty)
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

        //turn ends
        TileHandler.InvokeOnPlayerTilePlaced();
    }

    readonly struct TileScore
    {
        public readonly int Row;
        public readonly int Col;
        public readonly float Score;
        public readonly BoardState Board;

        public TileScore(int row, int col, float score, BoardState board
        )
        {
            this.Row = row;
            this.Col = col;
            this.Score = score;
            this.Board = board;
        }
    }

    readonly struct AsyncProps
    {
        public readonly int Row;
        public readonly int Col;
        public readonly BoardState BoardStateCopy;
        public readonly List<TileScore> TileScores;

        public AsyncProps(int row, int col, BoardState boardStateCopy, List<TileScore> tileScores)
        {
            this.Row = row;
            this.Col = col;
            this.BoardStateCopy = boardStateCopy;
            this.TileScores = tileScores;
        }
    }

    private void SmartAI()
    {
        //store best move to place next tile as row and col
        Dictionary<string, int> bestMove = new();
        //create simple copy of the board state as integers to reduce complexity of placing and removing tiles as game objects
        BoardState originalBoardState = new(_playingField.TileMatrix, _playingField.PlayerPerTile);

        List<TileScore> tileScores = new();

        List<Tuple<int, int, int>> possibleMoves = new();

        for (int row = 0; row < _playingField.GridWidth; row++)
        {
            for (int col = 0; col < _playingField.GridWidth; col++)
            {
                //skip if tile is not empty
                if (_playingField.PlayerPerTile[_playingField.TileMatrix[row, col]] != null)
                    continue;

                possibleMoves.Add(new Tuple<int, int, int>(row, col, 0));
            }
        }

        var tracker = Stopwatch.StartNew();

        foreach (var move in possibleMoves)
        {
            int row = move.Item1;
            int col = move.Item2;

            ThreadPool.QueueUserWorkItem((worker =>
            {
                AsyncProps asyncProps = (AsyncProps)worker;
                //set current player and round for copied board state to simulate next move
                asyncProps.BoardStateCopy.CurrentPlayer = _playingField.CurrentPlayer;
                asyncProps.BoardStateCopy.CurrentRound = _playingField.Round + 1;

                //place tile at current test position in copied board
                asyncProps.BoardStateCopy.Board[asyncProps.Row, asyncProps.Col] = asyncProps.BoardStateCopy.CurrentPlayer;

                BoardTree currentNode = asyncProps.BoardStateCopy.CurrentNode;
                if (GameManager.Instance.EnableLogging)
                    asyncProps.BoardStateCopy.AddStateToTree();


                //get score for that new board state
                float score = MiniMax(asyncProps.BoardStateCopy, 0, false,
                    asyncProps.BoardStateCopy.CurrentPlayer,
                    asyncProps.BoardStateCopy.CurrentRound);

                asyncProps.BoardStateCopy.CurrentNode.Score = score;
                asyncProps.BoardStateCopy.CurrentNode = currentNode;

                //remove reference from copied board to reverse changes
                asyncProps.BoardStateCopy.Board[asyncProps.Row, asyncProps.Col] = 0;

                lock (asyncProps.TileScores)
                    asyncProps.TileScores.Add(new TileScore(asyncProps.Row, asyncProps.Col, score, asyncProps.BoardStateCopy));
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
        _durationOfAlgorithm = tracker.ElapsedMilliseconds;

        Debug.Log("Max Depth: " + _maxDepth);
        Debug.Log(_durationOfAlgorithm + " ms");

        TileScore? bestTile = null;
        tileScores.ForEach(tileScore =>
        {
            if (bestTile == null || tileScore.Score > bestTile.Value.Score)
                bestTile = tileScore;
        });
        List<TileScore> tileScoresClone = tileScores;
        tileScoresClone.RemoveAll(tileScore => tileScore.Score != bestTile.Value.Score);
        bestTile = tileScoresClone[Random.Range(0, tileScoresClone.Count)];

        if (_currentDifficulty == AIDifficulty.OptimalWithRandomness)
        {
            tileScoresClone = tileScores;
            tileScoresClone.RemoveAll(tileScore => tileScore.Score == bestTile.Value.Score);
            TileScore randomNonOptimal = tileScoresClone[Random.Range(0, tileScoresClone.Count)];
            if (Random.Range(1, 11) <= 3)
            {
                bestTile = randomNonOptimal;
            }
        }

        bestMove["row"] = bestTile.Value.Row;
        bestMove["col"] = bestTile.Value.Col;

        if (GameManager.Instance.EnableLogging)
        {
            tileScores.ForEach((tilescore) => tilescore.Board.PrintTree());
        }
        GameObject bestMoveTile = _playingField.TileMatrix[bestMove["row"], bestMove["col"]];
        bestMoveTile.GetComponentInChildren<TileHandler>().PlaceTile(bestMoveTile.transform.GetChild(0));
    }

    private float MiniMax(BoardState board, int depth, bool isMaximizing, int currentPlayer, int currentRound)
    {
        if (depth > _maxDepth)
            _maxDepth = depth;

        int winner = board.CheckForWin();

        if (winner == 0 && board.CurrentRound == board.Board.GetLength(0) * board.Board.GetLength(1))
            return 0;

        if (winner != 0)
        {
            int score = winner == _playingField.CurrentPlayer ? 1 : -1;
            return score;
        }

        if (depth >= _configuredMaxDepth)
        {
            return board.CheckForPrematureScore(isMaximizing);
        }

        board.CurrentPlayer = currentPlayer != GameManager.Instance.PlayerCount ? currentPlayer + 1 : 1;
        board.CurrentRound = currentRound + 1;

        List<Tuple<int, int, int>> possibleMoves = new();

        if (isMaximizing)
        {
            //bestScore is initially the worst possible score, so its updated only when its better
            float bestScore = -1;
            float bestPossibleScore = 1;

            for (int row = 0; row < board.Board.GetLength(0); row++)
            {
                for (int col = 0; col < board.Board.GetLength(0); col++)
                {
                    //skip if tile is not empty
                    if (board.Board[row, col] != 0) 
                        continue;

                    //place tile and get score for that new board state
                    board.Board[row, col] = board.CurrentPlayer;

                    int possibleTerminalScore = board.CheckForWin();
                    if (possibleTerminalScore != 0)
                        possibleTerminalScore = possibleTerminalScore == _playingField.CurrentPlayer ? 1 : -1;

                    board.Board[row, col] = 0;

                    possibleMoves.Add(new Tuple<int, int, int>(row, col, possibleTerminalScore));
                }
            }

            possibleMoves.Sort((x, y) => y.Item3.CompareTo(x.Item3));

            foreach (var move in possibleMoves)
            {
                //place tile and get score for that new board state
                board.Board[move.Item1, move.Item2] = board.CurrentPlayer;

                BoardTree currentNode = board.CurrentNode;
                if (GameManager.Instance.EnableLogging)
                    board.AddStateToTree();

                float score = MiniMax(board, depth + 1, false, board.CurrentPlayer, board.CurrentRound);
                board.CurrentNode.Score = score;
                board.CurrentNode = currentNode;

                //destroy tile and remove reference from grid to reverse changes
                board.Board[move.Item1, move.Item2] = 0;

                //update best score
                bestScore = Math.Max(score, bestScore);
                if (score >= bestPossibleScore)
                    return score;
                
            }

            return bestScore;
        }
        else
        {
            //bestScore is initially the worst possible score, so its updated only when its better
            float bestScore = 1;
            float bestPossibleScore = -1;

            for (int row = 0; row < board.Board.GetLength(0); row++)
            {
                for (int col = 0; col < board.Board.GetLength(0); col++)
                {
                    //skip if tile is not empty
                    if (board.Board[row, col] != 0) 
                        continue;

                    //place tile and get score for that new board state
                    board.Board[row, col] = board.CurrentPlayer;

                    int possibleTerminalScore = board.CheckForWin();
                    if (possibleTerminalScore != 0)
                        possibleTerminalScore = possibleTerminalScore == _playingField.CurrentPlayer ? 1 : -1;

                    board.Board[row, col] = 0;

                    possibleMoves.Add(new Tuple<int, int, int>(row, col, possibleTerminalScore));
                }
            }

            possibleMoves.Sort((x, y) => x.Item3.CompareTo(y.Item3));

            foreach (var move in possibleMoves)
            {
                //place tile and get score for that new board state
                board.Board[move.Item1, move.Item2] = board.CurrentPlayer;

                BoardTree currentNode = board.CurrentNode;
                if (GameManager.Instance.EnableLogging)
                    board.AddStateToTree();

                float score = MiniMax(board, depth + 1, true, board.CurrentPlayer, board.CurrentRound);
                board.CurrentNode.Score = score;
                board.CurrentNode = currentNode;

                //destroy tile and remove reference from grid to reverse changes
                board.Board[move.Item1, move.Item2] = 0;

                bestScore = Math.Min(score, bestScore);
                if (score >= bestPossibleScore)
                    return score;
            }
            return bestScore;
        }
    }

    private void DumbAI()
    {
        //get all empty tiles
        List<GameObject> emptyTiles = new();
        for (int row = 0; row < _playingField.GridWidth; row++)
        {
            for (int col = 0; col < _playingField.GridWidth; col++)
            {
                if (_playingField.PlayerPerTile[_playingField.TileMatrix[row, col]] == null)
                    emptyTiles.Add(_playingField.TileMatrix[row, col]);
            }
        }
        //get random empty tile and place player tile with reference in grid
        GameObject emptyTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
        emptyTile.GetComponentInChildren<TileHandler>().PlaceTile(emptyTile.transform.GetChild(0));
    }

}

public class BoardState
{
    public int[,] Board;

    public int CurrentRound;

    public int CurrentPlayer;

    private readonly BoardTree _root;

    public BoardTree CurrentNode;

    private readonly Grid _playingField = GameManager.Instance.PlayingField;


    public BoardState(GameObject[,] tileMatrix, Dictionary<GameObject, GameObject> playerPerTile)
    {
        Board = new int[tileMatrix.GetLength(0), tileMatrix.GetLength(1)];
        for (int row = 0; row < tileMatrix.GetLength(0); row++)
        {
            for (int col = 0; col < tileMatrix.GetLength(1); col++)
            {
                Board[row, col] = playerPerTile[tileMatrix[row, col]] == null
                    ? 0
                    : Int32.Parse(playerPerTile[tileMatrix[row, col]].GetComponent<Image>().sprite.name[6].ToString());
            }
        }

        _root = new BoardTree();
        CurrentNode = _root;
    }

    public BoardState(BoardState boardState)
    {
        Board = (int[,])boardState.Board.Clone();
        CurrentRound = boardState.CurrentRound;
        CurrentPlayer = boardState.CurrentPlayer;
        _root = new BoardTree();
        CurrentNode = _root;
    }

    public void AddStateToTree()
    {
        CurrentNode = CurrentNode.AddChild(CopyBoardState());
    }

    public int[,] CopyBoardState() => (int[,])Board.Clone();

    public void PrintTree()
    {
        Debug.Log(_root.PrintTree());
    }

    public int CheckForWin()
    {
        int horizontalWin = CheckForHorizontalWin();
        int verticalWin = CheckForVerticalWin();
        int diagonalWin = CheckForDiagonalWin();
        if (horizontalWin != 0) return horizontalWin;
        if (verticalWin != 0) return verticalWin;
        if (diagonalWin != 0) return diagonalWin;
        return 0;
    }

    private int CheckForHorizontalWin()
    {
        for (int row = 0; row < Board.GetLength(0); row++)
        {
            if (Board[row, 0] == 0) continue;
            int playerSymbol = Board[row, 0];

            int playerTilesInRow = 1;
            for (int col = 1; col < Board.GetLength(1); col++)
            {
                if (Board[row, col] == 0)
                    break;
                if (playerSymbol != Board[row, col])
                    break;
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
            if (Board[0, col] == 0) continue;
            int playerSymbol = Board[0, col];

            int playerTilesInCol = 1;
            for (int row = 1; row < Board.GetLength(0); row++)
            {
                if (Board[row, col] == 0) break;
                if (playerSymbol != Board[row, col]) break;
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
        if (Board[0, 0] != 0)
        {
            int playerSymbol = Board[0, 0];
            int playerTilesInDiagonal = 1;
            for (int i = 1; i < Board.GetLength(0); i++)
            {
                if (Board[i, i] == 0) break;
                if (playerSymbol != Board[i, i]) break;
                playerTilesInDiagonal++;
            }

            if (playerTilesInDiagonal == Board.GetLength(0))
            {
                return playerSymbol;
            }
        }

        if (Board[0, Board.GetLength(1) - 1] != 0)
        {
            int playerSymbol = Board[0, Board.GetLength(1) - 1];
            int playerTilesInDiagonal = 1;
            for (int i = 1; i < Board.GetLength(0); i++)
            {
                if (Board[i, Board.GetLength(1) - 1 - i] == 0) break;
                if (playerSymbol != Board[i, Board.GetLength(1) - 1 - i]) break;
                playerTilesInDiagonal++;
            }

            if (playerTilesInDiagonal == Board.GetLength(0))
            {
                return playerSymbol;
            }
        }

        return 0;
    }

    public float CheckForPrematureScore(bool isMaximizing)
    {
        float horizontalPrematureScore = CheckForHorizontalPrematureScore();
        float verticalPrematureScore = CheckForVerticalPrematureScore();
        float diagonalPrematureScore = CheckForDiagonalPrematureScore();
        if (isMaximizing)
            return Math.Max(Math.Max(horizontalPrematureScore, verticalPrematureScore), diagonalPrematureScore);
        else
            return Math.Min(Math.Min(horizontalPrematureScore, verticalPrematureScore), diagonalPrematureScore);
    }

    private readonly HashSet<int> _alreadyNotPossibleRows = new();

    private float CheckForHorizontalPrematureScore()
    {
        Dictionary<int, float> rowScoresForPlayers = new();
        List<int> playersWithAlmostTerminalRows = new();
        
        for (int row = 0; row < Board.GetLength(0); row++)
        {
            if (_alreadyNotPossibleRows.Contains(row))
                break;

            int playerSymbol = 0;
            int playerTilesInRow = 0;

            for (int col = 0; col < Board.GetLength(1); col++)
            {
                if (playerSymbol != 0 && playerSymbol != Board[row, col])
                {
                    _alreadyNotPossibleRows.Add(row);
                    break;
                }

                if (playerSymbol == 0 && Board[row, col] != 0)
                    playerSymbol = Board[row, col];

                playerTilesInRow++;
            }

            rowScoresForPlayers.TryAdd(playerSymbol, 0);
            rowScoresForPlayers[playerSymbol] += (float) playerTilesInRow / Board.GetLength(0);
            if (playerTilesInRow == Board.GetLength(0) - 1)
                playersWithAlmostTerminalRows.Add(playerSymbol);

        }

        if (playersWithAlmostTerminalRows.Count > 0)
        {
            if (playersWithAlmostTerminalRows.Exists(player =>
                    CurrentPlayer == _playingField.CurrentPlayer &&
                    player != _playingField.CurrentPlayer))
            {
                return -1;
            }

            if (playersWithAlmostTerminalRows.Exists(player =>
                    CurrentPlayer != _playingField.CurrentPlayer &&
                    player == _playingField.CurrentPlayer &&
                    (CurrentPlayer != GameManager.Instance.PlayerCount ? CurrentPlayer + 1 : 1) ==
                    _playingField.CurrentPlayer))
            {
                return 1;
            }
        }

        float overallScore = 0;
        foreach (var playerScore in rowScoresForPlayers.Values)
            overallScore += playerScore;

        return overallScore;
    }

    private readonly HashSet<int> _alreadyNotPossibleCols = new();

    private float CheckForVerticalPrematureScore()
    {
        Dictionary<int, float> colScoresForPlayers = new();
        List<int> playersWithAlmostTerminalCols = new();

        for (int col = 0; col < Board.GetLength(0); col++)
        {
            if (_alreadyNotPossibleCols.Contains(col))
                break;

            int playerSymbol = 0;
            int playerTilesInCol = 0;

            for (int row = 0; row < Board.GetLength(1); row++)
            {
                if (playerSymbol != 0 && playerSymbol != Board[row, col])
                {
                    _alreadyNotPossibleCols.Add(col);
                    break;
                }
                    
                if (playerSymbol == 0 && Board[row, col] != 0)
                    playerSymbol = Board[row, col];

                playerTilesInCol++;
            }

            colScoresForPlayers.TryAdd(playerSymbol, 0);
            colScoresForPlayers[playerSymbol] += (float) playerTilesInCol / Board.GetLength(0);
            if (playerTilesInCol == Board.GetLength(0) - 1)
                playersWithAlmostTerminalCols.Add(playerSymbol);

        }

        if (playersWithAlmostTerminalCols.Count > 0)
        {
            if (playersWithAlmostTerminalCols.Exists(player =>
                    CurrentPlayer == _playingField.CurrentPlayer &&
                    player != _playingField.CurrentPlayer))
            {
                return -1;
            }

            if (playersWithAlmostTerminalCols.Exists(player =>
                    CurrentPlayer != _playingField.CurrentPlayer &&
                    player == _playingField.CurrentPlayer &&
                    (CurrentPlayer != GameManager.Instance.PlayerCount ? CurrentPlayer + 1 : 1) ==
                    _playingField.CurrentPlayer))
            {
                return 1;
            }
        }

        float overallScore = 0;
        foreach (var playerScore in colScoresForPlayers.Values)
            overallScore += playerScore;

        return overallScore;
    }

    private readonly HashSet<int> _alreadyNotPossibleDiagonals = new();

    private float CheckForDiagonalPrematureScore()
    {
        Dictionary<int, float> diagonalScoresForPlayers = new();
        List<int> playersWithAlmostTerminalDiagonals = new();

        {
            if (!_alreadyNotPossibleDiagonals.Contains(1))
            {


                int playerSymbol = 0;
                int playerTilesInDiagonal = 0;
                for (int i = 0; i < Board.GetLength(0); i++)
                {
                    if (playerSymbol != 0 && playerSymbol != Board[i, i])
                    {
                        _alreadyNotPossibleDiagonals.Add(1);
                        break;
                    }

                    if (playerSymbol == 0 && Board[i, i] != 0)
                        playerSymbol = Board[i, i];

                    playerTilesInDiagonal++;
                }

                diagonalScoresForPlayers.TryAdd(playerSymbol, 0);
                diagonalScoresForPlayers[playerSymbol] += (float)playerTilesInDiagonal / Board.GetLength(0);
                if (playerTilesInDiagonal == Board.GetLength(0) - 1)
                    playersWithAlmostTerminalDiagonals.Add(playerSymbol);
            }
        }

        
        {
            if (!_alreadyNotPossibleDiagonals.Contains(2))
            {

                int playerSymbol = 0;
                int playerTilesInDiagonal = 0;
                for (int i = 0; i < Board.GetLength(0); i++)
                {
                    int colPosition = Board.GetLength(0) - 1 - i;
                    if (playerSymbol != 0 && playerSymbol != Board[i, colPosition])
                    {
                        _alreadyNotPossibleDiagonals.Add(2);
                        break;
                    }

                    if (playerSymbol == 0 && Board[i, colPosition] != 0)
                        playerSymbol = Board[i, colPosition];

                    playerTilesInDiagonal++;
                }

                diagonalScoresForPlayers.TryAdd(playerSymbol, 0);
                diagonalScoresForPlayers[playerSymbol] += (float)playerTilesInDiagonal / Board.GetLength(0);
                if (playerTilesInDiagonal == Board.GetLength(0) - 1)
                    playersWithAlmostTerminalDiagonals.Add(playerSymbol);
            }
        }

        if (playersWithAlmostTerminalDiagonals.Count > 0)
        {
            if (playersWithAlmostTerminalDiagonals.Exists(player =>
                    CurrentPlayer == _playingField.CurrentPlayer &&
                    player != _playingField.CurrentPlayer))
            {
                return -1;
            }

            if (playersWithAlmostTerminalDiagonals.Exists(player =>
                    CurrentPlayer != _playingField.CurrentPlayer &&
                    player == _playingField.CurrentPlayer &&
                    (CurrentPlayer != GameManager.Instance.PlayerCount ? CurrentPlayer + 1 : 1) ==
                    _playingField.CurrentPlayer))
            {
                return 1;
            }
        }

        float overallScore = 0;
        foreach (var playerScore in diagonalScoresForPlayers.Values)
            overallScore += playerScore;

        return overallScore;
    }
}
