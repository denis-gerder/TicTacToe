using System.Collections;
using TicTacToe;
using UnityEngine;
using Grid = TicTacToe.Grid;

namespace Scripts
{
    public class AIConfigurator : MonoBehaviour
    {
        public static AIConfigurator Instance;

        public AIConfigSO _aIConfigSO;

        [SerializeField]
        private GameObject _visibleScreen;

        [SerializeField]
        private GameObject _tilePrefab;

        private Grid _testGrid;

        private Transform _testGridGO;

        private float _timeForAI;

        private bool _gotDuration = false;

        private void Awake()
        {
            Instance = this;
            InitConfigureScreen();
        }

        public void ReconfigureAI()
        {
            gameObject.SetActive(true);
            _visibleScreen.SetActive(false);
            _aIConfigSO.AIConfigurated = false;
            InitConfigureScreen();
        }

        private void InitConfigureScreen()
        {
            if (!_aIConfigSO.AIConfigurated)
                StartCoroutine(ConfigureAI());
            else
            {
                gameObject.SetActive(false);
                _visibleScreen.SetActive(true);
            }
        }

        private IEnumerator ConfigureAI()
        {
            _timeForAI = float.MaxValue;
            _testGrid = new(
                transform.parent.gameObject,
                _tilePrefab,
                new GameConfig(2, 10, true, AIDifficulty.Optimal),
                transform
            );
            _testGridGO = transform.Find("Grid");
            Debug.Log("allo");
            _testGridGO.SetSiblingIndex(0);
            TileHandler.OnPlayerTilePlaced += HandlePlayerTilePlaced;
            while (_timeForAI > 100f || _timeForAI < 10f)
            {
                _testGrid.Clear();
                _testGridGO
                    .GetChild(0)
                    .GetComponent<TileHandler>()
                    .PlaceTile(_testGridGO.GetChild(0));
                yield return new WaitUntil(() => _gotDuration);
                if (_timeForAI > 100f)
                    _aIConfigSO.ConfiguratedMaxDepth--;
                else if (_timeForAI > 10f)
                    _aIConfigSO.ConfiguratedMaxDepth++;
            }

            _aIConfigSO.AIConfigurated = true;
            Destroy(_testGridGO.gameObject);
            gameObject.SetActive(false);
            _visibleScreen.SetActive(true);
            yield return null;
        }

        private void HandlePlayerTilePlaced(int currentPlayer)
        {
            if (currentPlayer <= 1)
                return;
            _timeForAI = _testGridGO.GetComponent<EnemyAI>().DurationOfAlgorithm;
            _gotDuration = true;
        }
    }
}
