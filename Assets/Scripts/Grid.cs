using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Grid
{
    //singleton instance of the grid
    public static GameObject GridInstance;

    public static Dictionary<GameObject, GameObject> PlayerPerTile = new();

    public static GameObject[,] TileMatrix;
    
    public Grid(GameObject canvas, GameObject tilePrefab, int gridWidth)
    {
        GridInstance = Object.Instantiate(new GameObject("Grid"), canvas.transform);
        
        TileMatrix = new GameObject[gridWidth, gridWidth];
        
        float tileScale = 1f / gridWidth * 8;
        float tileWidth = tilePrefab.GetComponent<RectTransform>().rect.width;
        
        for (int row = 0; row < gridWidth; row++)
        {
            for (int col = 0; col < gridWidth; col++)
            {
                GameObject tileInstance = Object.Instantiate(tilePrefab, GridInstance.transform);
                PlayerPerTile.Add(tileInstance, null);
                TileMatrix[row, col] = tileInstance;
                
                RectTransform rectTransform = tileInstance.GetComponent<RectTransform>();
                rectTransform.localScale *= tileScale;
                rectTransform.anchoredPosition = new Vector3(tileScale * row * tileWidth, tileScale * col * tileWidth, 0);
            }
        }
        
        CenterGrid(gridWidth, tileWidth, tileScale);
    }

    private void CenterGrid(int gridWidth, float tileWidth, float tileScale)
    {
        GridInstance.transform.localPosition = new Vector3(
            -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 
            -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 
            0);
    }
}
