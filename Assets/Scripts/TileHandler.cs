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
    public class TileHandler
        : MonoBehaviour,
            IPointerEnterHandler,
            IPointerDownHandler,
            IPointerExitHandler
    {
        [SerializeField]
        private GameObject _playerTilePrefab;
        public PlayerModelsSO PlayerModelsSo;

        [SerializeField]
        private Material _mouseOverMaterial;
        private readonly float _maximumMouseOverOpacity = 0.5f;
        private readonly float _fadeDuration = 0.2f;
        private readonly Func<float, float> _easingFunction = x =>
            (float)-(Math.Cos(Math.PI * x) - 1) / 2;
        public static event Action<int> OnPlayerTilePlaced;
        private Board _playingField;
        private MeshRenderer _thisMeshRenderer;
        private static List<MeshRenderer> _playerModelMeshRenderers;
        private static List<MeshFilter> _playerModelMeshFilters;
        private MouseOverInfo _currentMouseOver;
        private CoroutineManager _coroutineManager;

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
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            //do not show visual if tile is already occupied
            if (_playingField.PlayerPerTile[gameObject] != null)
                return;

            _playingField.MouseOverObjects.ForEach(mouseOver =>
                UnityEngine.Debug.Log(JsonUtility.ToJson(mouseOver, true))
            );

            bool wereCoroutinesRunning = _coroutineManager.AreCoroutinesRunning();
            _coroutineManager.StopAndClearCoroutines();

            if (_currentMouseOver == null || !wereCoroutinesRunning)
                _currentMouseOver =
                    _playingField.MouseOverObjects.Find(mouseOver => !mouseOver.IsCurrentlyUsed)
                    ?? _playingField.IncreaseMouseOverObjects();

            _coroutineManager.EnqueueSequentally(
                lockMouseOver(_currentMouseOver),
                move(
                    transform.localPosition
                        + new Vector3(
                            0,
                            _thisMeshRenderer.bounds.size.y / 2
                                + _playerModelMeshRenderers[_playingField.CurrentPlayer - 1]
                                    .bounds
                                    .size
                                    .y / 2,
                            0
                        ),
                    _currentMouseOver.MouseOverObject.transform
                ),
                setMeshFilter(
                    _currentMouseOver.MeshFilter,
                    _playerModelMeshFilters[_playingField.CurrentPlayer - 1].sharedMesh
                ),
                setMeshRenderer(
                    _currentMouseOver.MeshRenderer,
                    _playerModelMeshRenderers[_playingField.CurrentPlayer - 1].sharedMaterial
                ),
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _currentMouseOver.MeshRenderer.material,
                    _easingFunction,
                    _fadeDuration,
                    maxValue: _maximumMouseOverOpacity,
                    fieldsToChange: "a"
                )
            );
        }

        private IEnumerator move(Vector3 position, Transform transform)
        {
            transform.localPosition = position;
            yield break;
        }

        private IEnumerator setMeshFilter(MeshFilter meshFilter, Mesh mesh)
        {
            meshFilter.mesh = mesh;
            yield break;
        }

        private IEnumerator setMeshRenderer(MeshRenderer meshRenderer, Material material)
        {
            meshRenderer.materials.Append(material);
            yield break;
        }

        private IEnumerator lockMouseOver(MouseOverInfo _currentMouseOver)
        {
            _currentMouseOver.IsCurrentlyUsed = true;
            yield break;
        }

        private IEnumerator freeMouseOver(MouseOverInfo _currentMouseOver)
        {
            _currentMouseOver.IsCurrentlyUsed = false;
            yield break;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //do not show visual if tile is already occupied
            if (_playingField.PlayerPerTile[gameObject] != null)
                return;

            _coroutineManager.EnqueueSequentally(
                move(
                    transform.localPosition
                        + new Vector3(
                            0,
                            _thisMeshRenderer.bounds.size.y / 2
                                + _playerModelMeshRenderers[_playingField.CurrentPlayer - 1]
                                    .bounds
                                    .size
                                    .y / 2,
                            0
                        ),
                    _currentMouseOver.MouseOverObject.transform
                ),
                setMeshFilter(
                    _currentMouseOver.MeshFilter,
                    _playerModelMeshFilters[_playingField.CurrentPlayer - 1].sharedMesh
                ),
                setMeshRenderer(
                    _currentMouseOver.MeshRenderer,
                    _playerModelMeshRenderers[_playingField.CurrentPlayer - 1].sharedMaterial
                ),
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _currentMouseOver.MeshRenderer.material,
                    x => 1 - _easingFunction(x),
                    _fadeDuration,
                    maxValue: _maximumMouseOverOpacity,
                    fieldsToChange: "a"
                ),
                freeMouseOver(_currentMouseOver)
            );
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //return if tile is already occupied or if AI is enabled and it's the AI's turn
            if (
                (_playingField.GameConfig.AIEnabled && _playingField.CurrentPlayer != 1)
                || _playingField.PlayerPerTile[gameObject] != null
                || GameManager.GameOver
            )
                return;

            //spawn player tile and set reference to tile in grid
            PlaceTile(transform);
        }

        public void PlaceTile(Transform parentTransform)
        {
            GameObject playerTile = Instantiate(_playerTilePrefab, parentTransform);

            GameObject playerModel = Instantiate(
                PlayerModelsSo.PlayerSymbols[_playingField.CurrentPlayer - 1],
                playerTile.transform
            );

            _playingField.PlayerPerTile[gameObject] = new PlayerInfo(
                _playingField.CurrentPlayer,
                playerTile
            );
            OnPlayerTilePlaced?.Invoke(_playingField.CurrentPlayer);

            _coroutineManager.StopAndClearCoroutines();

            _coroutineManager.Enqueueparallel(
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    _currentMouseOver.MeshRenderer.material,
                    x => 1 - _easingFunction(x),
                    _fadeDuration,
                    maxValue: _maximumMouseOverOpacity,
                    fieldsToChange: "a"
                ),
                AnimationUtils.EasePropertyFloatOnObject(
                    playerTile.transform,
                    "localPosition",
                    x => 1 - _easingFunction(x),
                    0.5f,
                    transform.GetComponentInChildren<MeshRenderer>().bounds.size.y / 2
                        + playerModel.GetComponentInChildren<MeshRenderer>().bounds.size.y / 2,
                    (
                        transform.GetComponentInChildren<MeshRenderer>().bounds.size.y / 2
                        + playerModel.GetComponentInChildren<MeshRenderer>().bounds.size.y / 2
                    ) * 5,
                    "y"
                ),
                AnimationUtils.EasePropertyFloatOnObject<Color>(
                    playerTile.GetComponentInChildren<MeshRenderer>().material,
                    _easingFunction,
                    _fadeDuration,
                    fieldsToChange: "a"
                )
            );
            _coroutineManager.EnqueueSequentally(freeMouseOver(_currentMouseOver));
        }
    }
}
