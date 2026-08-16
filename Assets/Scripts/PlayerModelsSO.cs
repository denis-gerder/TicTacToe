using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlayerModelsSO",
    menuName = "ScriptableObjects/PlayerModelsSO",
    order = 2
)]
public class PlayerModelsSO : ScriptableObject
{
    public List<GameObject> PlayerSymbols;

    public List<RuntimeAnimatorController> AnimationControllersPerPlayer;
}
