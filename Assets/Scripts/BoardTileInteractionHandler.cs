using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

namespace TicTacToe
{
    public class BoardTileInteractionHandler
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerDownHandler,
            IPointerExitHandler
    {
        [SerializeField]
        private GameObject _playerTilePrefab;

        [SerializeField]
        private Material _mouseOverMaterial;

        public PlayerModelsSO PlayerModelsSo;

        public event Action<int> OnPlayerTilePlaced;

        private readonly float _maximumMouseOverOpacity = 0.5f;
        private readonly float _fadeDuration = 0.2f;
        private readonly Func<float, float> _easingFunction = x =>
            (float)-(Math.Cos(Math.PI * x) - 1) / 2;

        private Board _playingField;
        private MeshRenderer _thisMeshRenderer;
        private static List<MeshRenderer> _playerModelMeshRenderers;
        private static List<MeshFilter> _playerModelMeshFilters;
        private MouseOverInfo _currentMouseOver;
        private CoroutineManager _coroutineManager;
        private bool _interactionAllowed;

        private void Start()
        {
            _coroutineManager = new(this);
            _thisMeshRenderer = gameObject.GetComponentInChildren<MeshRenderer>();
            if (_playerModelMeshRenderers == null)
            {
                _playerModelMeshRenderers = new(PlayerModelsSo.PlayerSymbols.Count);
                foreach (GameObject playerModel in PlayerModelsSo.PlayerSymbols)
                {
                    _playerModelMeshRenderers.Add(
                        playerModel.GetComponentInChildren<MeshRenderer>()
                    );
                }
            }
            if (_playerModelMeshFilters == null)
            {
                _playerModelMeshFilters = new(PlayerModelsSo.PlayerSymbols.Count);
                foreach (GameObject playerModel in PlayerModelsSo.PlayerSymbols)
                {
                    _playerModelMeshFilters.Add(playerModel.GetComponentInChildren<MeshFilter>());
                }
            }
        }

        public void SetupPlayingFieldReference(Board playingField)
        {
            _playingField = playingField;
            _playingField.OnBoardSetupFinished += HandleBoardSetupFinished;
        }

        private void HandleBoardSetupFinished()
        {
            _interactionAllowed = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //do not show visual if tile is already occupied
            if (_playingField.PlayerPerTile[gameObject] != null || !_interactionAllowed)
                return;

            /* _playingField.MouseOverObjects.ForEach(mouseOver =>
                UnityEngine.Debug.Log(JsonUtility.ToJson(mouseOver, true))
            ); */

            bool wereCoroutinesRunning = _coroutineManager.AreCoroutinesRunning();
            _coroutineManager.StopAndClearCoroutines();

            if (_currentMouseOver == null || !wereCoroutinesRunning)
                _currentMouseOver =
                    _playingField.MouseOverObjects.Find(mouseOver => !mouseOver.IsCurrentlyUsed)
                    ?? _playingField.IncreaseMouseOverObjects();

            _currentMouseOver.IsCurrentlyUsed = true;
            _currentMouseOver.MouseOverObject.transform.localPosition =
                transform.localPosition
                + new Vector3(
                    0,
                    _thisMeshRenderer.bounds.size.y / 2
                        + _playerModelMeshRenderers[_playingField.CurrentPlayer - 1].bounds.size.y
                            / 2,
                    0
                );
            _currentMouseOver.MeshFilter.mesh = _playerModelMeshFilters[
                _playingField.CurrentPlayer - 1
            ].sharedMesh;
            _currentMouseOver.MeshRenderer.materials.Append(
                _playerModelMeshRenderers[_playingField.CurrentPlayer - 1].sharedMaterial
            );
            _coroutineManager.EnqueueSequentally(
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _currentMouseOver.MeshRenderer.material,
                    _easingFunction,
                    _fadeDuration,
                    maxValue: _maximumMouseOverOpacity,
                    fieldsToChange: "a"
                )
            );
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //do not show visual if tile is already occupied
            if (
                _playingField.PlayerPerTile[gameObject] != null
                || !_interactionAllowed
                || _currentMouseOver == null
            )
                return;

            _currentMouseOver.MouseOverObject.transform.localPosition =
                transform.localPosition
                + new Vector3(
                    0,
                    _thisMeshRenderer.bounds.size.y / 2
                        + _playerModelMeshRenderers[_playingField.CurrentPlayer - 1].bounds.size.y
                            / 2,
                    0
                );
            _currentMouseOver.MeshFilter.mesh = _playerModelMeshFilters[
                _playingField.CurrentPlayer - 1
            ].sharedMesh;
            _currentMouseOver.MeshRenderer.materials.Append(
                _playerModelMeshRenderers[_playingField.CurrentPlayer - 1].sharedMaterial
            );
            _coroutineManager.EnqueueSequentally(
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _currentMouseOver.MeshRenderer.material,
                    x => 1 - _easingFunction(x),
                    _fadeDuration,
                    maxValue: _maximumMouseOverOpacity,
                    fieldsToChange: "a"
                ),
                AnimationUtils.ActionAsCoroutine(() => _currentMouseOver.IsCurrentlyUsed = false)
            );
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //return if tile is already occupied or if AI is enabled and it's the AI's turn
            if (
                (_playingField.GameConfig.AIEnabled && _playingField.CurrentPlayer != 1)
                || _playingField.PlayerPerTile[gameObject] != null
                || GameManager.GameOver
                || !_interactionAllowed
            )
                return;

            //spawn player tile and set reference to tile in grid
            PlaceTile(transform);
        }

        public void PlaceTile(Transform parentTransform)
        {
            GameObject playerTile = Instantiate(_playerTilePrefab, parentTransform);

            playerTile.transform.localPosition =
                PlayerModelsSo
                    .PlayerSymbols[_playingField.CurrentPlayer - 1]
                    .transform
                    .localPosition
                + new Vector3(
                    0,
                    GetComponentInChildren<MeshFilter>().mesh.bounds.size.y / 2
                        + _thisMeshRenderer.bounds.size.y,
                    0
                );
            playerTile.GetComponent<MeshFilter>().mesh = _playerModelMeshFilters[
                _playingField.CurrentPlayer - 1
            ].sharedMesh;
            playerTile.GetComponent<MeshRenderer>().material.mainTexture =
                _playerModelMeshRenderers[_playingField.CurrentPlayer - 1]
                    .sharedMaterial
                    .mainTexture;
            Animator playerTileAnimator = playerTile.GetComponent<Animator>();
            playerTileAnimator.runtimeAnimatorController =
                PlayerModelsSo.AnimationControllersPerPlayer[_playingField.CurrentPlayer - 1];

            _playingField.PlayerPerTile[gameObject] = new PlayerInfo(
                _playingField.CurrentPlayer,
                playerTile
            );

            if (_currentMouseOver != null)
                _coroutineManager.EnqueueSequentally(
                    AnimationUtils.EasePropertyFloatOnObject<Color>(
                        _currentMouseOver.MeshRenderer.material,
                        x => 1 - _easingFunction(x),
                        _fadeDuration,
                        maxValue: _maximumMouseOverOpacity,
                        fieldsToChange: "a"
                    ),
                    AnimationUtils.ActionAsCoroutine(
                        () => _currentMouseOver.IsCurrentlyUsed = false
                    )
                );

            OnPlayerTilePlaced?.Invoke(_playingField.CurrentPlayer);
        }
    }
}
