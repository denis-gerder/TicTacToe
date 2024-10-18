using UnityEngine;

[CreateAssetMenu(fileName = "GameConfigSO", menuName = "ScriptableObjects/GameConfigSO", order = 1)]
public class GameConfigSO : ScriptableObject
{
    public int PlayerAmount;

    public int BoardSize;

    public bool AIEnabled;

    public AIDifficulty AIDifficulty;
}
