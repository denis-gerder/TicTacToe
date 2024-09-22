using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileHandler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler
{
    [SerializeField] 
    private GameObject _playerPrefab;
    
    [SerializeField] 
    public PlayerConfigSO PlayerConfigSo;
    
    private Image _mouseOverVisual;

    //Opacity of the Mouse Over Visual in percent
    private const int MouseOverOpacity = 50;
    
    //Intensity of the Mouse Over Visual (How many steps it takes to fade in/out)
    private const int MouseOverFadeIntensity = 10;
    
    public static event Action OnPlayerTilePlaced;

    private Grid _playingField;
    
    private void Awake()
    {
        _mouseOverVisual = gameObject.transform.GetChild(0).GetComponent<Image>();
    }

    public void SetupPlayingFieldReference(Grid playingField)
    {
        _playingField = playingField;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //do not show visual if tile is already occupied
        if(_playingField.PlayerPerTile[gameObject] != null) 
            return;
        StartCoroutine(FadeInVisual(_mouseOverVisual));
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        //do not show visual if tile is already occupied
        if(_playingField.PlayerPerTile[gameObject] != null) 
            return;
        StartCoroutine(FadeOutVisual(_mouseOverVisual));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        //return if tile is already occupied or if AI is enabled and it's the AI's turn
        if ((GameManager.Instance.IsAiEnabled && _playingField.CurrentPlayer != 1) || _playingField.PlayerPerTile[gameObject] != null || GameManager.Instance.GameOver) 
            return;
        
        //spawn player tile and set reference to tile in grid
        PlaceTile(transform);

        StartCoroutine(FadeOutVisual(_mouseOverVisual));
    }

    public void PlaceTile(Transform parentTransform)
    {
        GameObject playerTile = Instantiate(_playerPrefab, parentTransform);
        playerTile.GetComponent<Image>().sprite = PlayerConfigSo.PlayerSymbols[_playingField.CurrentPlayer - 1];
        _playingField.PlayerPerTile[gameObject] = playerTile;
        OnPlayerTilePlaced?.Invoke();
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
