using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverVisual : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        StartCoroutine(FadeInVisual(GetComponent<SpriteRenderer>()));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Clicked on tile");
    }

    private IEnumerator FadeInVisual(SpriteRenderer spriteRenderer)
    {
        for (float i = 0; i < 1; i += 0.1f)
        {
            spriteRenderer.color = new Color(1,1,1,i);
            yield return new WaitForSeconds(0.1f);
        }
    }

    
}
