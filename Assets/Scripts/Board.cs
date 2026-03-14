using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TicTacToe
{
    public readonly struct PlayerInfo
    {
        public PlayerInfo(int player, GameObject playerTile)
            : this()
        {
            Player = player;
            PlayerTile = playerTile;
        }

        public readonly int Player;
        public readonly GameObject PlayerTile;
    }

    public class MouseOverInfo
    {
        public bool IsCurrentlyUsed;
        public readonly GameObject MouseOverObject;
        public readonly MeshRenderer MeshRenderer;
        public readonly MeshFilter MeshFilter;

        public MouseOverInfo(GameObject mouseOverObject)
        {
            IsCurrentlyUsed = false;
            MouseOverObject = mouseOverObject;
            MeshRenderer = mouseOverObject.GetComponent<MeshRenderer>();
            MeshFilter = mouseOverObject.GetComponent<MeshFilter>();
        }
    }

    public class Board : MonoBehaviour
    {
        [SerializeField]
        private GameObject _boardTilePrefab;

        [SerializeField]
        private GameObject _mouseOverPrefab;

        public GameObject Canvas { get; private set; }

        public int CurrentPlayer { get; private set; } = 1;
        public int CurrentRound { get; private set; } = 1;
        public GameConfigSO GameConfig;

        private GameObject _gridInstance;

        //dictionary to keep track of which player placed a tile on which tile
        public Dictionary<GameObject, PlayerInfo?> PlayerPerTile = new();

        //matrix to keep track of the tiles
        public GameObject[,] TileMatrix { get; private set; }
        public List<MouseOverInfo> MouseOverObjects { get; private set; } = new();
        public event Action<bool, int> OnGameOver;
        public event Action OnTurnEnd;

        //percentage 0.0f - 1.0f
        private readonly float _percentageGridToScreen = 0.7f;
        private readonly float _percentageGridToScreenHeight = 0.8f;

        private void Start()
        {
            int gridWidth = GameConfig.BoardSize;
            float tileWidth = _boardTilePrefab
                .GetComponentInChildren<MeshFilter>()
                .sharedMesh.bounds.size.x;
            _gridInstance = new GameObject("Grid");
            _gridInstance.transform.SetParent(transform, false);
            IncreaseMouseOverObjects();
            IncreaseMouseOverObjects();
            TileMatrix = new GameObject[gridWidth, gridWidth];

            for (int row = 0; row < gridWidth; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject tileInstance = Instantiate(
                        _boardTilePrefab,
                        _gridInstance.transform
                    );
                    TileHandler tileHandlerInstance =
                        tileInstance.GetComponentInChildren<TileHandler>();
                    tileHandlerInstance.SetupPlayingFieldReference(this);

                    PlayerPerTile.Add(tileInstance, null);
                    TileMatrix[row, col] = tileInstance;

                    tileInstance.transform.localPosition = new Vector3(
                        row * tileWidth,
                        0,
                        col * tileWidth
                    );
                }
            }

            CenterGrid(gridWidth, tileWidth, 1);

            TileHandler.OnPlayerTilePlaced += HandlePlayerTilePlaced;
            _gridInstance.AddComponent<EnemyAI>().SetupPlayingFieldReference(this);
        }

        public MouseOverInfo IncreaseMouseOverObjects()
        {
            MouseOverInfo mouseOverInfo =
                new(Instantiate(_mouseOverPrefab, _gridInstance.transform));
            MouseOverObjects.Add(mouseOverInfo);
            return mouseOverInfo;
        }

        public void SetCanvas(GameObject canvasObject)
        {
            Canvas = canvasObject;
            Canvas.GetComponentInChildren<PlayerHolderHandler>().SetupPlayingFieldReference(this);
        }

        public void Clear()
        {
            for (int i = 0; i < TileMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < TileMatrix.GetLength(1); j++)
                {
                    if (PlayerPerTile[TileMatrix[i, j]] == null)
                    {
                        continue;
                    }
                    Object.Destroy(PlayerPerTile[TileMatrix[i, j]]?.PlayerTile);
                    PlayerPerTile[TileMatrix[i, j]] = null;
                }
            }
        }

        private void CenterGrid(int gridWidth, float tileWidth, float tileScale)
        {
            float pixelGridWidth = gridWidth * tileScale * tileWidth;
            float screenHeight = pixelGridWidth / _percentageGridToScreen;
            _gridInstance.transform.localPosition = new Vector3(
                (-pixelGridWidth / 2f) + (tileScale * tileWidth / 2f),
                0,
                (-pixelGridWidth / 2f)
                    + (tileScale * tileWidth / 2f)
                    - (
                        screenHeight
                        * (1 - _percentageGridToScreen)
                        / 2f
                        * ((_percentageGridToScreenHeight * 2) - 1)
                    )
            );
        }

        private void HandlePlayerTilePlaced(int currentPlayer)
        {
            //check if game is won or if it's a draw
            int winner = CheckForWin();
            if (winner != 0)
                OnGameOver?.Invoke(true, winner);

            if (CurrentRound == GameConfig.BoardSize * GameConfig.BoardSize && winner == 0)
                OnGameOver?.Invoke(false, CurrentPlayer);

            CurrentRound++;
            CurrentPlayer = currentPlayer != GameConfig.PlayerAmount ? currentPlayer + 1 : 1;

            //end turn
            if (_gridInstance != null)
                OnTurnEnd?.Invoke();
        }

        public int CheckForWin()
        {
            int horizontalWin = CheckForHorizontalWin();
            int verticalWin = CheckForVerticalWin();
            int diagonalWin = CheckForDiagonalWin();
            if (horizontalWin != 0)
                return horizontalWin;
            if (verticalWin != 0)
                return verticalWin;
            if (diagonalWin != 0)
                return diagonalWin;
            return 0;
        }

        private int CheckForHorizontalWin()
        {
            int gridWidth = GameConfig.BoardSize;
            for (int row = 0; row < gridWidth; row++)
            {
                if (PlayerPerTile[TileMatrix[row, 0]] == null)
                    continue;
                int player = PlayerPerTile[TileMatrix[row, 0]].Value.Player;

                int playerTilesInRow = 1;
                for (int col = 1; col < gridWidth; col++)
                {
                    if (PlayerPerTile[TileMatrix[row, col]] == null)
                        break;
                    if (player != PlayerPerTile[TileMatrix[row, col]]?.Player)
                    {
                        break;
                    }

                    playerTilesInRow++;
                }
                if (playerTilesInRow == gridWidth)
                {
                    return player;
                }
            }
            return 0;
        }

        private int CheckForVerticalWin()
        {
            int gridWidth = GameConfig.BoardSize;
            for (int col = 0; col < gridWidth; col++)
            {
                if (PlayerPerTile[TileMatrix[0, col]] == null)
                    continue;
                int player = PlayerPerTile[TileMatrix[0, col]].Value.Player;

                int playerTilesInCol = 1;
                for (int row = 1; row < gridWidth; row++)
                {
                    if (PlayerPerTile[TileMatrix[row, col]] == null)
                        break;
                    if (player != PlayerPerTile[TileMatrix[row, col]]?.Player)
                    {
                        break;
                    }

                    playerTilesInCol++;
                }

                if (playerTilesInCol == gridWidth)
                {
                    return player;
                }
            }
            return 0;
        }

        private int CheckForDiagonalWin()
        {
            int gridWidth = GameConfig.BoardSize;
            int playerTilesInDgl1 = 0;
            int player = 0;
            if (PlayerPerTile[TileMatrix[0, 0]] != null)
            {
                int player1 = PlayerPerTile[TileMatrix[0, 0]].Value.Player;

                playerTilesInDgl1 = 1;

                for (int row = 1; row < gridWidth; row++)
                {
                    if (
                        PlayerPerTile[TileMatrix[row, row]] == null
                        || player1 != PlayerPerTile[TileMatrix[row, row]]?.Player
                    )
                        break;

                    playerTilesInDgl1++;
                }

                if (playerTilesInDgl1 == gridWidth)
                {
                    player = player1;
                }
            }

            int playerTilesInDgl2 = 0;
            if (PlayerPerTile[TileMatrix[0, gridWidth - 1]] != null)
            {
                int player2 = PlayerPerTile[TileMatrix[0, gridWidth - 1]].Value.Player;
                playerTilesInDgl2 = 1;

                for (int row = 1; row < gridWidth; row++)
                {
                    if (
                        PlayerPerTile[TileMatrix[row, gridWidth - row - 1]] == null
                        || player2 != PlayerPerTile[TileMatrix[row, gridWidth - row - 1]]?.Player
                    )
                    {
                        break;
                    }

                    playerTilesInDgl2++;
                }

                if (playerTilesInDgl2 == gridWidth)
                {
                    player = player2;
                }
            }
            if (playerTilesInDgl1 == gridWidth || playerTilesInDgl2 == gridWidth)
                return player;

            return player;
        }
    }
}
