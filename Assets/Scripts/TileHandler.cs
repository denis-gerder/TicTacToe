using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileHandler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    [SerializeField] private GameObject playerPrefab;
    
    private Image _image;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartCoroutine(FadeInVisual(_image));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        StartCoroutine(FadeOutVisual(_image));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        GameObject playerSymbol = Instantiate(playerPrefab, transform);
        Grid.PlayerPerTile[gameObject] = playerSymbol;
        
        
        if(GameManager.Instance.CurrentPlayer != GameManager.Instance.PlayerCount) GameManager.Instance.CurrentPlayer++;
        else GameManager.Instance.CurrentPlayer = 1;
    }

    private IEnumerator FadeInVisual(Image image)
    {
        for (float i = 0; i <= 10; i++)
        {
            Color tempColor = image.color;
            tempColor.a = i/10;
            image.color = tempColor;
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    private IEnumerator FadeOutVisual(Image image)
    {
        for (float i = 10; i >= 0; i--)
        {
            Color tempColor = image.color;
            tempColor.a = i/10;
            image.color = tempColor;
            yield return new WaitForSeconds(0.01f);
        }
    }
}
