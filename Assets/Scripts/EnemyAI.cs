using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

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
        
        //create simple copy of the board state as integers to reduce complexity of placing and removing tiles as game objects
        BoardState originalBoardState = new(_playingField.TileMatrix, _playingField.PlayerPerTile);
        originalBoardState.CurrentPlayer = _playingField.CurrentPlayer;
        originalBoardState.CurrentRound = _playingField.Round;

        List<Tuple<int, int, int>> possibleMoves = new();
        for (int row = 0; row < originalBoardState.Board.GetLength(0); row++)
        {
            for (int col = 0; col < originalBoardState.Board.GetLength(1); col++)
            {
                //skip if tile is not empty
                if (originalBoardState.Board[row, col] != 0)
                    continue;

                possibleMoves.Add(new Tuple<int, int, int>(row, col, 0));
            }
        }

        var tracker = Stopwatch.StartNew();

        List<TileScore> tileScores = new();
        foreach (var move in possibleMoves)
        {
            int row = move.Item1;
            int col = move.Item2;

            ThreadPool.QueueUserWorkItem(worker =>
            {
                AsyncProps asyncProps = (AsyncProps)worker;

                //maximizing = true because its reverted in function (maximizing bezieht sich eigentlich auf den gesamten Zug vorher und die KI will ja eigentlich maximieren)
                SetupAndDoMiniMaxForMove(asyncProps.BoardStateCopy, move, -1, true, out float score);

                lock (asyncProps.TileScores)
                    asyncProps.TileScores.Add(new TileScore(asyncProps.Row, asyncProps.Col, score, asyncProps.BoardStateCopy));
            }, new AsyncProps(row, col, new BoardState(originalBoardState), tileScores));
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

        Dictionary<string, int> bestMove = new()
        {
            ["row"] = bestTile.Value.Row,
            ["col"] = bestTile.Value.Col
        };

        if (GameManager.Instance.EnableLogging)
            tileScores.ForEach((tilescore) => tilescore.Board.PrintTree());

        GameObject bestMoveTile = _playingField.TileMatrix[bestMove["row"], bestMove["col"]];
        bestMoveTile.GetComponent<TileHandler>().PlaceTile(bestMoveTile.transform);
    }

    private void SetupAndDoMiniMaxForMove(BoardState board, Tuple<int, int, int> move, int depth, bool isMaximizing, out float score) 
    {
        //place tile and get score for that new board state
        board.Board[move.Item1, move.Item2] = board.CurrentPlayer;

        BoardTree currentNode = board.CurrentNode;
        if (GameManager.Instance.EnableLogging)
            board.AddStateToTree();

        score = MiniMax(board, depth + 1, !isMaximizing, board.CurrentPlayer, board.CurrentRound);
        board.CurrentNode.Score = score;
        board.CurrentNode = currentNode;

        //destroy tile and remove reference from grid to reverse changes
        board.Board[move.Item1, move.Item2] = 0;
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
                SetupAndDoMiniMaxForMove(board, move, depth, true, out float score);

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
                SetupAndDoMiniMaxForMove(board, move, depth, false, out float score);

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
        emptyTile.GetComponent<TileHandler>().PlaceTile(emptyTile.transform);
    }

}
