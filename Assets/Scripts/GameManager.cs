using TMPro;
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

        [SerializeField]
        private GameObject _boardTilePrefab;

        [SerializeField]
        private GameObject _boardPrefab;
        public GameConfigSO _gameConfigSO;

        [SerializeField]
        private GameObject _endScreen;

        [SerializeField]
        private TMP_Text _endText;

        [SerializeField]
        private GameObject _mouseOverPrefab;
        private GameConfig _gameConfig;

        [HideInInspector]
        public static bool GameOver { get; private set; }

        [SerializeField]
        private bool _enableLogging;
        private Board _playingField;

        [HideInInspector]
        public bool EnableLogging
        {
            get { return _enableLogging; }
            private set { _enableLogging = value; }
        }

        //Spawn Grid and populate player array
        private void Start()
        {
            Instance = this;
            _gameConfig = new GameConfig(
                _gameConfigSO.PlayerAmount,
                _gameConfigSO.BoardSize,
                _gameConfigSO.AIEnabled,
                _gameConfigSO.AIDifficulty
            );

            _playingField = Instantiate(_boardPrefab).GetComponent<Board>();
            _playingField.SetCanvas(_canvas);
            _playingField.OnGameOver += HandleGameOver;
        }

        private void OnDestroy()
        {
            GameOver = false;
            Instance._playingField.OnGameOver -= HandleGameOver;
        }

        private void HandleGameOver(bool isGameWon, int player)
        {
            GameOver = true;
            Instance._endScreen.SetActive(true);
            if (isGameWon)
            {
                Instance._endText.text = $"Player {player} won the game!";
                Debug.Log($"Player {player} won the game!");
            }
            else
            {
                Instance._endText.text = "Draw!";
                Debug.Log("Draw!");
            }
        }
    }

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
