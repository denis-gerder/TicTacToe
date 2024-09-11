using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileHandler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    [SerializeField] 
    private GameObject _playerPrefab;
    
    [SerializeField] 
    public PlayerConfigSO PlayerConfigSo;
    
    private Image _image;
    
    protected bool GameOver = false;

    //Opacity of the Mouse Over Visual in percent
    private const int MouseOverOpacity = 50;
    
    //Intensity of the Mouse Over Visual (How many steps it takes to fade in/out)
    private const int MouseOverFadeIntensity = 10;
    
    public static event Action OnPlayerTilePlaced;

    private Grid _playingField;
    
    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void SetupPlayingFieldReference(Grid playingField)
    {
        _playingField = playingField;
    }

    public static void InvokeOnPlayerTilePlaced()
    {
        OnPlayerTilePlaced?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //do not show visual if tile is already occupied
        if(_playingField.PlayerPerTile[gameObject.transform.parent.gameObject] != null) 
            return;
        StartCoroutine(FadeInVisual(_image));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        //do not show visual if tile is already occupied
        if(_playingField.PlayerPerTile[gameObject.transform.parent.gameObject] != null) 
            return;
        StartCoroutine(FadeOutVisual(_image));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        //return if tile is already occupied or if AI is enabled and it's the AI's turn
        if ((GameManager.Instance.IsAiEnabled && _playingField.CurrentPlayer != 1) || _playingField.PlayerPerTile[gameObject.transform.parent.gameObject] != null || GameOver) 
            return;
        
        //spawn player tile and set reference to tile in grid
        PlaceTile(transform);
        
        StartCoroutine(FadeOutVisual(_image));

        //turn ends and next player is set

        InvokeOnPlayerTilePlaced();
    }

    public void PlaceTile(Transform parentTransform)
    {
        GameObject playerTile = Instantiate(_playerPrefab, parentTransform);
        playerTile.GetComponent<Image>().sprite = PlayerConfigSo.PlayerSymbols[_playingField.CurrentPlayer - 1];
        _playingField.PlayerPerTile[gameObject.transform.parent.gameObject] = playerTile;
    }

    private IEnumerator FadeInVisual(Image image)
    {
        for (float i = 0; i <= MouseOverOpacity / MouseOverFadeIntensity; i++)
        {
            Color tempColor = image.color;
            tempColor.a = i/10;
            image.color = tempColor;
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    private IEnumerator FadeOutVisual(Image image)
    {
        for (float i = MouseOverOpacity / MouseOverFadeIntensity; i >= 0; i--)
        {
            Color tempColor = image.color;
            tempColor.a = i/10;
            image.color = tempColor;
            yield return new WaitForSeconds(0.01f);
        }
    }
}
