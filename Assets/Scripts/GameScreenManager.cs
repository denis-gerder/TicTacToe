using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TicTacToe
{
    public class GameScreenManager : CommonScreenManager
    {
        private readonly Func<float, float> _easingFunction = x => x * x * x; //1 - (float)Math.Pow(1 - x, 3)

        public void StartBoardEndingAnimationAndChangeScene(string scene)
        {
            _currentScreen.SetActive(false);
            Board board = GameManager.Instance.GetPlayingField();
            int gridWidth = board.GameConfig.BoardSize;
            Mesh boardTileMesh = board.TileMatrix[0, 0].GetComponentInChildren<MeshFilter>().mesh;
            float halfGridLengthPosition =
                board.TileMatrix[0, 0].transform.position.x
                - boardTileMesh.bounds.size.x / 2
                + boardTileMesh.bounds.size.x * gridWidth / 2;
            Vector3 explosionCenter = new(halfGridLengthPosition, -1, halfGridLengthPosition);
            List<IEnumerator> tileCoroutines = new();
            foreach (var tile in board.PlayerPerTile.Keys)
            {
                if (tile.transform.childCount == 1)
                    continue;
                Transform playerTile = tile.transform.GetChild(1);
                Vector3 endPosition =
                    (tile.transform.localPosition - explosionCenter).normalized * 10
                    + playerTile.transform.localPosition;
                tileCoroutines.Add(
                    AnimationUtils.EasePropertyFloatOnObject(
                        playerTile.transform,
                        "localPosition",
                        _easingFunction,
                        1f,
                        new PropertyToAnimate(
                            "x",
                            playerTile.transform.localPosition.x,
                            endPosition.x
                        ),
                        new PropertyToAnimate(
                            "y",
                            playerTile.transform.localPosition.y,
                            endPosition.y
                        ),
                        new PropertyToAnimate(
                            "z",
                            playerTile.transform.localPosition.z,
                            endPosition.z
                        )
                    )
                );
            }
            _coroutineManager
                .EnqueueParallel(tileCoroutines.ToArray())
                .EnqueueSequentally(AnimationUtils.ActionAsCoroutine(() => ChangeScene(scene)));
        }
    }
}
