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

    private int[] _players;
    public int[] Players => _players;

    //Spawn Grid and populate player array
    private void Awake()
    {
        Grid grid = new Grid(canvas, tilePrefab, width);
        _players = new int[playerCount];
        for(int i = 1; i <= playerCount; i++) _players[i-1] = i;
    }
    
}
