using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Scripts;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TicTacToe
{
    public enum AIDifficulty
    {
        Random,
        Dumb,
        OptimalWithRandomness,
        Optimal,
    }

    public class EnemyAI : MonoBehaviour
    {
        private AIDifficulty _currentDifficulty;
        public long DurationOfAlgorithm { get; private set; }
        public int _configuredMaxDepth;
        private Grid _playingField;
        private bool _enableLogging;
        private static readonly object _listLock = new();

        public void SetupPlayingFieldReference(Grid playingField)
        {
            if (GameManager.Instance != null)
                _enableLogging = GameManager.Instance.EnableLogging;
            _playingField = playingField;
            _currentDifficulty = _playingField.GameConfig.AIDifficulty;
            _playingField.OnTurnEnd += HandleTurnEnd;
        }

        private void HandleTurnEnd()
        {
            //return if AI is disabled or if it's not the AI's turn
            if (
                !_playingField.GameConfig.AIEnabled
                || _playingField.CurrentPlayer == 1
                || GameManager.GameOver
            )
                return;

            if (_currentDifficulty == AIDifficulty.Random)
            {
                List<AIDifficulty> allDifficulties = new();
                allDifficulties.AddRange(
                    Enum.GetValues(typeof(AIDifficulty)).OfType<AIDifficulty>()
                );
                allDifficulties.Remove(AIDifficulty.Random);
                _currentDifficulty = allDifficulties[Random.Range(0, allDifficulties.Count)];
            }

            //ai places tile
            switch (_currentDifficulty)
            {
                case AIDifficulty.Dumb:
                    StartCoroutine(DumbAI());
                    break;
                case AIDifficulty.Optimal:
                    StartCoroutine(SmartAI());
                    break;
                case AIDifficulty.OptimalWithRandomness:
                    StartCoroutine(SmartAI());
                    break;
            }
        }

        readonly struct TileScore
        {
            public readonly int Row;
            public readonly int Col;
            public readonly float Score;
            public readonly BoardState Board;

            public TileScore(int row, int col, float score, BoardState board)
            {
                this.Row = row;
                this.Col = col;
                this.Score = score;
                this.Board = board;
            }
        }

        private IEnumerator SmartAI()
        {
            if (AIConfigurator.Instance != null)
                _configuredMaxDepth = AIConfigurator.Instance._aIConfigSO.ConfiguratedMaxDepth;
            //create simple copy of the board state as integers to reduce complexity of placing and removing tiles as game objects
            BoardState originalBoardState =
                new(_playingField.TileMatrix, _playingField.PlayerPerTile, _playingField);

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
            int toProcess = possibleMoves.Count;

            foreach (var move in possibleMoves)
            {
                int row = move.Item1;
                int col = move.Item2;

                Task.Run(() =>
                {
                    BoardState boardStateCopy = new(originalBoardState);
                    SetupAndDoMiniMaxForMove(boardStateCopy, move, -1, true, out float score);

                    lock (_listLock)
                    {
                        tileScores.Add(new TileScore(row, col, score, boardStateCopy));
                        toProcess--;
                    }
                });
            }

            yield return new WaitUntil(() => toProcess == 0);

            tracker.Stop();
            DurationOfAlgorithm = tracker.ElapsedMilliseconds;
            UnityEngine.Debug.Log(DurationOfAlgorithm);

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
                TileScore randomNonOptimal = tileScoresClone[
                    Random.Range(0, tileScoresClone.Count)
                ];
                if (Random.Range(1, 11) <= 3)
                    bestTile = randomNonOptimal;
            }

            Dictionary<string, int> bestMove =
                new() { ["row"] = bestTile.Value.Row, ["col"] = bestTile.Value.Col };

            if (_enableLogging)
                tileScores.ForEach((tilescore) => tilescore.Board.PrintTree());

            GameObject bestMoveTile = _playingField.TileMatrix[bestMove["row"], bestMove["col"]];
            bestMoveTile.GetComponent<TileHandler>().PlaceTile(bestMoveTile.transform);
        }

        private void SetupAndDoMiniMaxForMove(
            BoardState board,
            Tuple<int, int, int> move,
            int depth,
            bool isMaximizing,
            out float score
        )
        {
            //place tile and get score for that new board state
            board.Board[move.Item1, move.Item2] = board.CurrentPlayer;

            BoardTree currentNode = board.CurrentNode;
            if (_enableLogging)
                board.AddStateToTree();

            score = MiniMax(
                board,
                depth + 1,
                !isMaximizing,
                board.CurrentPlayer,
                board.CurrentRound
            );
            board.CurrentNode.Score = score;
            board.CurrentNode = currentNode;

            //destroy tile and remove reference from grid to reverse changes
            board.Board[move.Item1, move.Item2] = 0;
        }

        private float MiniMax(
            BoardState board,
            int depth,
            bool isMaximizing,
            int currentPlayer,
            int currentRound
        )
        {
            int winner = board.CheckForWin();

            if (
                winner == 0
                && board.CurrentRound == board.Board.GetLength(0) * board.Board.GetLength(1)
            )
                return 0;

            if (winner != 0)
                return winner == _playingField.CurrentPlayer ? 1 : -1;

            //for now disabled because persisting data is not implemented yet and the configurated ai depth would be reset on every game startup because of the incorrect usage of scriptable objects
            //if (depth >= _configuredMaxDepth)
            //return board.CheckForPrematureScore(isMaximizing);

            board.CurrentPlayer =
                currentPlayer != _playingField.GameConfig.PlayerAmount ? currentPlayer + 1 : 1;
            board.CurrentRound = currentRound + 1;

            List<Tuple<int, int, int>> possibleMoves = new();

            if (isMaximizing)
            {
                //bestScore is initially the worst possible score, so its updated only when its better
                float bestScore = -1;
                const float bestPossibleScore = 1;

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
                        {
                            possibleTerminalScore =
                                possibleTerminalScore == _playingField.CurrentPlayer ? 1 : -1;
                        }

                        board.Board[row, col] = 0;

                        possibleMoves.Add(
                            new Tuple<int, int, int>(row, col, possibleTerminalScore)
                        );
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
                const float bestPossibleScore = -1;

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
                        {
                            possibleTerminalScore =
                                possibleTerminalScore == _playingField.CurrentPlayer ? 1 : -1;
                        }

                        board.Board[row, col] = 0;

                        possibleMoves.Add(
                            new Tuple<int, int, int>(row, col, possibleTerminalScore)
                        );
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

        private IEnumerator DumbAI()
        {
            //get all empty tiles
            List<GameObject> emptyTiles = new();
            for (int row = 0; row < _playingField.GameConfig.BoardSize; row++)
            {
                for (int col = 0; col < _playingField.GameConfig.BoardSize; col++)
                {
                    if (_playingField.PlayerPerTile[_playingField.TileMatrix[row, col]] == null)
                        emptyTiles.Add(_playingField.TileMatrix[row, col]);
                }
            }
            //get random empty tile and place player tile with reference in grid
            GameObject emptyTile = emptyTiles[Random.Range(0, emptyTiles.Count)];
            emptyTile.GetComponent<TileHandler>().PlaceTile(emptyTile.transform);

            yield return null;
        }
    }
}
