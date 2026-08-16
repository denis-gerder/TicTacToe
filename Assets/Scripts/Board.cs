using System;
using System.Collections;
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
        private CoroutineManager _coroutineManager;

        //dictionary to keep track of which player placed a tile on which tile
        public Dictionary<GameObject, PlayerInfo?> PlayerPerTile = new();

        //matrix to keep track of the tiles
        public GameObject[,] TileMatrix { get; private set; }
        public List<MouseOverInfo> MouseOverObjects { get; private set; } = new();
        public event Action<bool, int> OnGameOver;
        public event Action OnTurnEnd;
        public event Action OnBoardSetupFinished;

        //percentage 0.0f - 1.0f
        private readonly float _percentageWidthGridToScreen = 0.7f;
        private readonly Func<float, float> _easingFunction = x => 1 - (float)Math.Pow(1 - x, 3);

        private void Start()
        {
            _coroutineManager = new(this);
            Mesh tileMesh = _boardTilePrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            int gridWidth = GameConfig.BoardSize;
            float tileWidth = tileMesh.bounds.size.x;
            GameObject boardFloor = transform.GetChild(0).gameObject;
            MeshFilter floorMeshFilter = boardFloor.GetComponent<MeshFilter>();
            float floorHeight = floorMeshFilter.sharedMesh.bounds.size.y;
            float floorWidth = floorMeshFilter.sharedMesh.bounds.size.x;
            float floorScale = gridWidth * tileWidth / floorWidth * 1.1f;
            boardFloor.transform.localScale = new Vector3(floorScale, 1, floorScale);
            boardFloor.transform.localPosition = new Vector3(
                tileWidth * gridWidth / 2 - tileWidth / 2,
                -floorHeight / 2,
                tileWidth * gridWidth / 2 - tileWidth / 2
            );
            _gridInstance = new GameObject("Grid");
            _gridInstance.transform.SetParent(transform, false);
            TileMatrix = new GameObject[gridWidth, gridWidth];

            Mesh floorMesh = boardFloor.GetComponent<MeshFilter>().sharedMesh;

            for (int row = 0; row < gridWidth; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject tileInstance = Instantiate(
                        _boardTilePrefab,
                        new Vector3(
                            row * tileWidth,
                            boardFloor.transform.position.y
                                - ((floorMesh.bounds.size.y / 2) + (tileMesh.bounds.size.y / 2)),
                            col * tileWidth
                        ),
                        new Quaternion(),
                        _gridInstance.transform
                    );
                    _coroutineManager.EnqueueSequentally(
                        AnimationUtils.EasePropertyFloatOnObject(
                            tileInstance.transform,
                            "localPosition",
                            _easingFunction,
                            0.15f,
                            tileInstance.transform.localPosition.y,
                            boardFloor.transform.position.y
                                + ((floorMesh.bounds.size.y / 2) + (tileMesh.bounds.size.y / 2)),
                            "y"
                        )
                    );
                    BoardTileInteractionHandler tileHandlerInstance =
                        tileInstance.GetComponentInChildren<BoardTileInteractionHandler>();
                    tileHandlerInstance.SetupPlayingFieldReference(this);
                    tileHandlerInstance.OnPlayerTilePlaced += HandlePlayerTilePlaced;

                    PlayerPerTile.Add(tileInstance, null);
                    TileMatrix[row, col] = tileInstance;
                }
            }

            CenterCameraOverBoard(gridWidth, tileWidth);

            _coroutineManager.EnqueueSequentally(
                AnimationUtils.ActionAsCoroutine(() => OnBoardSetupFinished?.Invoke())
            );

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
                    Destroy(PlayerPerTile[TileMatrix[i, j]]?.PlayerTile);
                    PlayerPerTile[TileMatrix[i, j]] = null;
                }
            }
        }

        private void CenterCameraOverBoard(int gridWidth, float tileWidth)
        {
            float halfGridLengthPosition =
                _gridInstance.transform.position.x - tileWidth / 2 + gridWidth * tileWidth / 2;
            Vector3 targetPosition =
                new(
                    halfGridLengthPosition,
                    _gridInstance.transform.position.y,
                    0.8f * halfGridLengthPosition //0.8f - magic number that also centers vertically
                );
            Camera camera = Camera.main;
            float distance =
                (1 / _percentageWidthGridToScreen * tileWidth * gridWidth)
                / (2 * Mathf.Tan(camera.fieldOfView * Mathf.PI / 360));
            camera.transform.position = targetPosition - (camera.transform.forward * distance);
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
