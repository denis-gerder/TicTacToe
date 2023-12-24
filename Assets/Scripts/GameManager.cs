using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Singleton reference
    public static GameManager Instance;

    [SerializeField] private GameObject canvas;
    
    [SerializeField] GameObject tilePrefab;
    
    [SerializeField] [Range(2, 10)] private int width = 3;
    
    [SerializeField] [Range(2, 5)] private int playerCount = 2;
    public int PlayerCount => playerCount;

    [HideInInspector] public int currentPlayer = 1;

    [HideInInspector] public int round = 0;

    //Spawn Grid and populate player array
    private void Awake()
    {
        Instance = this;
        Grid grid = new Grid(canvas, tilePrefab, width);
        Grid.OnGameOver += HandleGameOver;
    }
    
    private void HandleGameOver(bool isGameWon)
    {
        if(isGameWon) Debug.Log($"Player {currentPlayer} won the game!");
        else Debug.Log("Draw!");
    }
    
    
    
}
