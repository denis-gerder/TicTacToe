using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerConfigSo",
    menuName = "ScriptableObjects/PlayerConfigSo",
    order = 2
)]
public class PlayerConfigSO : ScriptableObject
{
    public List<Sprite> PlayerSymbols;
}
