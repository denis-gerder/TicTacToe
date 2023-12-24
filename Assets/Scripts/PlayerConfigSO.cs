using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfigSO", menuName = "ScriptableObjects/PlayerConfigSO", order = 1)]
public class PlayerConfigSO : ScriptableObject
{
    public List<PlayerSymbol> PlayerSymbols;
}
[Serializable]
public class PlayerSymbol
{
    public Sprite Sprite;
    public Color Color;
}
