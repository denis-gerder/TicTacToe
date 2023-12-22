using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class Grid
{
    //singleton instance of the grid
    public static GameObject GridInstance;
    
    private Vector2 _tileScale;
    
    public Grid(GameObject canvas, GameObject tilePrefab, int gridWidth)
    {
        GridInstance = Object.Instantiate(new GameObject("Grid"), canvas.transform);
        
        
        for (int col = 0; col < gridWidth; col++)
        {
            for (int row = 0; row < gridWidth; row++)
            {
                GameObject tileInstance = Object.Instantiate(tilePrefab, GridInstance.transform);
                
                RectTransform rectTransform = tileInstance.GetComponent<RectTransform>();
                
                rectTransform.localScale *= 1f / gridWidth * 8;
                _tileScale = rectTransform.localScale;
                
                float tileSize = rectTransform.rect.width;
                rectTransform.localPosition = new Vector3(_tileScale.x * row + tileSize, _tileScale.y * col + tileSize, 0);
            }
        }
        
        CenterGrid(gridWidth);
    }

    private void CenterGrid(int width)
    {
        GridInstance.transform.position = new Vector3(-(width * _tileScale.x / 2f - _tileScale.x / 2f), -(width * _tileScale.y / 2f - _tileScale.y / 2f), 0);
    }
}
