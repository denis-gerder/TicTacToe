using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfigSo", menuName = "ScriptableObjects/PlayerConfigSo", order = 1)]
public class PlayerConfigSO : ScriptableObject
{
    public List<Sprite> PlayerSymbols;
}