using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Singleton reference
    public static GameManager Instance;

    [SerializeField] 
    private GameObject _canvas;
    
    [SerializeField] 
    private GameObject _tilePrefab;

    [SerializeField][Range(2, 10)] 
    private int _width = 3;
    
    [SerializeField] [Range(2, 5)] 
    public int PlayerCount = 2;

    [SerializeField] 
    public bool IsAiEnabled;

    [SerializeField]
    public AIDifficulty AiDifficulty;

    [SerializeField]
    public bool EnableLogging;

    public Grid PlayingField { get; private set; }

    [HideInInspector] 
    public bool GameOver { get; private set; }

    //Spawn Grid and populate player array
    private void Awake()
    {
        Instance = this;
        PlayingField = new Grid(_canvas, _tilePrefab, _width);
        PlayingField.OnGameOver += HandleGameOver;
    }
    
    private void HandleGameOver(bool isGameWon, int player)
    {
        GameOver = true;
        if(isGameWon) Debug.Log($"Player {player} won the game!");
        else Debug.Log("Draw!");
    }
}
