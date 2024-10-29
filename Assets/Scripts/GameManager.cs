using System;
using UnityEngine;

namespace TicTacToe
{
    public class GameManager : MonoBehaviour
    {
        //Singleton reference
        public static GameManager Instance;

        [SerializeField]
        private GameObject _canvas;

        [SerializeField]
        private GameObject _tilePrefab;

        public GameConfigSO _gameConfigSO;

        [SerializeField]
        private GameConfig _gameConfig;

        [HideInInspector]
        public static bool GameOver { get; private set; }

        public static bool EnableLogging { get; private set; }

        //Spawn Grid and populate player array
        private void Awake()
        {
            Instance = this;
            _gameConfig = new GameConfig(
                _gameConfigSO.PlayerAmount,
                _gameConfigSO.BoardSize,
                _gameConfigSO.AIEnabled,
                _gameConfigSO.AIDifficulty
            );

            Grid playingField = new(_canvas, _tilePrefab, _gameConfig, _canvas.transform);
            playingField.OnGameOver += HandleGameOver;
        }

        private void HandleGameOver(bool isGameWon, int player)
        {
            GameOver = true;
            if (isGameWon)
                Debug.Log($"Player {player} won the game!");
            else
                Debug.Log("Draw!");
        }
    }

    [Serializable]
    public readonly struct GameConfig
    {
        public readonly int PlayerAmount;
        public readonly int BoardSize;
        public readonly bool AIEnabled;
        public readonly AIDifficulty AIDifficulty;

        public GameConfig(
            int playerAmount,
            int boardSize,
            bool AIEnabled,
            AIDifficulty AIDifficulty
        )
        {
            PlayerAmount = playerAmount;
            BoardSize = boardSize;
            this.AIEnabled = AIEnabled;
            this.AIDifficulty = AIDifficulty;
        }
    }
}
