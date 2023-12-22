using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Grid
{
    //singleton instance of the grid
    public static GameObject GridInstance;
    
    public Grid(GameObject canvas, GameObject tilePrefab, int gridWidth)
    {
        GridInstance = Object.Instantiate(new GameObject("Grid"), canvas.transform);
        
        float tileScale = 1f / gridWidth * 8;
        float tileWidth = tilePrefab.GetComponent<RectTransform>().rect.width;
        
        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridWidth; row++)
            {
                GameObject tileInstance = Object.Instantiate(tilePrefab, GridInstance.transform);
                
                RectTransform rectTransform = tileInstance.GetComponent<RectTransform>();
                rectTransform.localScale *= tileScale;
                rectTransform.anchoredPosition = new Vector3(tileScale * row * tileWidth, tileScale * col * tileWidth, 0);
            }
        }
        
        CenterGrid(gridWidth, tileWidth, tileScale);
    }

    private void CenterGrid(int gridWidth, float tileWidth, float tileScale)
    {
        GridInstance.transform.localPosition = new Vector3(-(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, -(gridWidth * tileScale * tileWidth / 2f) + tileScale * tileWidth / 2f, 0);
    }
}
