using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TicTacToe
{
    public class Grid
    {
        public int CurrentPlayer { get; private set; } = 1;

        public int CurrentRound { get; private set; } = 1;

        public readonly GameConfig GameConfig;

        //singleton instance of the grid
        private readonly GameObject _gridInstance;

        //dictionary to keep track of which player placed a tile on which tile
        public Dictionary<GameObject, GameObject> PlayerPerTile = new();

        //matrix to keep track of the tiles
        public GameObject[,] TileMatrix;

        public event Action<bool, int> OnGameOver;

        public event Action OnTurnEnd;

        //percentage 0.0f - 1.0f
        private readonly float _percentageGridToScreen = 0.7f;

        private readonly float _percentageGridToScreenHeight = 0.8f;

        public Grid(
            GameObject canvas,
            GameObject tilePrefab,
            GameConfig gameConfig,
            Transform parent
        )
        {
            GameConfig = gameConfig;
            int gridWidth = GameConfig.BoardSize;
            _gridInstance = new GameObject("Grid");
            _gridInstance.transform.SetParent(parent, false);
            _gridInstance.transform.SetAsFirstSibling();
            TileMatrix = new GameObject[gridWidth, gridWidth];

            float screenHeight = canvas.GetComponent<RectTransform>().rect.height;
            float tileWidth = tilePrefab.GetComponent<RectTransform>().rect.width;
            //float tileScale = 1f / gridWidth * 8;
            float tileScale = _percentageGridToScreen * screenHeight / gridWidth / tileWidth;

            for (int row = 0; row < gridWidth; row++)
            {
                for (int col = 0; col < gridWidth; col++)
                {
                    GameObject tileInstance = Object.Instantiate(
                        tilePrefab,
                        _gridInstance.transform
                    );
                    TileHandler tileHandlerInstance = tileInstance.GetComponent<TileHandler>();
                    tileHandlerInstance.SetupPlayingFieldReference(this);

                    PlayerPerTile.Add(tileInstance, null);
                    TileMatrix[row, col] = tileInstance;

                    RectTransform rectTransform = tileInstance.GetComponent<RectTransform>();
                    rectTransform.localScale = Vector3.one * tileScale;
                    rectTransform.anchoredPosition = new Vector3(
                        tileScale * col * tileWidth,
                        tileScale * row * tileWidth,
                        0
                    );
                }
            }

            CenterGrid(gridWidth, tileWidth, tileScale);

            TileHandler.OnPlayerTilePlaced += HandlePlayerTilePlaced;
            _gridInstance.AddComponent<EnemyAI>().SetupPlayingFieldReference(this);
            if (GameManager.Instance != null)
            {
                _gridInstance
                    .transform.parent.GetChild(1)
                    .GetComponent<PlayerHolderHandler>()
                    .SetupPlayingFieldReference(this);
            }
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
                    Object.Destroy(PlayerPerTile[TileMatrix[i, j]]);
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
                (-pixelGridWidth / 2f)
                    + (tileScale * tileWidth / 2f)
                    - (
                        screenHeight
                        * (1 - _percentageGridToScreen)
                        / 2f
                        * ((_percentageGridToScreenHeight * 2) - 1)
                    ),
                0
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
                Sprite playerSymbol = PlayerPerTile[TileMatrix[row, 0]]
                    .GetComponent<Image>()
                    .sprite;

                int playerTilesInRow = 1;
                for (int col = 1; col < gridWidth; col++)
                {
                    if (PlayerPerTile[TileMatrix[row, col]] == null)
                        break;
                    if (
                        playerSymbol
                        != PlayerPerTile[TileMatrix[row, col]].GetComponent<Image>().sprite
                    )
                    {
                        break;
                    }

                    playerTilesInRow++;
                }
                if (playerTilesInRow == gridWidth)
                {
                    int player =
                        PlayerPerTile[TileMatrix[row, 0]]
                            .transform.parent.GetComponent<TileHandler>()
                            .PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol) + 1;
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
                Sprite playerSymbol = PlayerPerTile[TileMatrix[0, col]]
                    .GetComponent<Image>()
                    .sprite;

                int playerTilesInCol = 1;
                for (int row = 1; row < gridWidth; row++)
                {
                    if (PlayerPerTile[TileMatrix[row, col]] == null)
                        break;
                    if (
                        playerSymbol
                        != PlayerPerTile[TileMatrix[row, col]].GetComponent<Image>().sprite
                    )
                    {
                        break;
                    }

                    playerTilesInCol++;
                }

                if (playerTilesInCol == gridWidth)
                {
                    int player =
                        PlayerPerTile[TileMatrix[0, col]]
                            .transform.parent.GetComponent<TileHandler>()
                            .PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol) + 1;
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
                Sprite playerSymbol1 = PlayerPerTile[TileMatrix[0, 0]].GetComponent<Image>().sprite;

                playerTilesInDgl1 = 1;

                for (int row = 1; row < gridWidth; row++)
                {
                    if (PlayerPerTile[TileMatrix[row, row]] == null)
                        break;
                    if (
                        playerSymbol1
                        != PlayerPerTile[TileMatrix[row, row]].GetComponent<Image>().sprite
                    )
                    {
                        break;
                    }

                    playerTilesInDgl1++;
                }

                if (playerTilesInDgl1 == gridWidth)
                {
                    player =
                        PlayerPerTile[TileMatrix[0, 0]]
                            .transform.parent.GetComponent<TileHandler>()
                            .PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol1) + 1;
                }
            }

            int playerTilesInDgl2 = 0;
            if (PlayerPerTile[TileMatrix[0, gridWidth - 1]] != null)
            {
                Sprite playerSymbol2 = PlayerPerTile[TileMatrix[0, gridWidth - 1]]
                    .GetComponent<Image>()
                    .sprite;

                playerTilesInDgl2 = 1;

                for (int row = 1; row < gridWidth; row++)
                {
                    if (PlayerPerTile[TileMatrix[row, gridWidth - row - 1]] == null)
                        break;
                    if (
                        playerSymbol2
                        != PlayerPerTile[TileMatrix[row, gridWidth - row - 1]]
                            .GetComponent<Image>()
                            .sprite
                    )
                    {
                        break;
                    }

                    playerTilesInDgl2++;
                }

                if (playerTilesInDgl2 == gridWidth)
                {
                    player =
                        PlayerPerTile[TileMatrix[0, gridWidth - 1]]
                            .transform.parent.GetComponent<TileHandler>()
                            .PlayerConfigSo.PlayerSymbols.IndexOf(playerSymbol2) + 1;
                }
            }
            if (playerTilesInDgl1 == gridWidth || playerTilesInDgl2 == gridWidth)
                return player;

            return player;
        }
    }
}
