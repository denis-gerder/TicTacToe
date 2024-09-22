using Assets.Scripts;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

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
            rowScoresForPlayers[playerSymbol] += (float)playerTilesInRow / Board.GetLength(0);
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
            colScoresForPlayers[playerSymbol] += (float)playerTilesInCol / Board.GetLength(0);
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