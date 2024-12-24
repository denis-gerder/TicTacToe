using UnityEngine;

[CreateAssetMenu(fileName = "AIConfigSO", menuName = "ScriptableObjects/AIConfigSO", order = 3)]
public class AIConfigSO : ScriptableObject
{
    public bool AIConfigurated = false;

    public int ConfiguratedMaxDepth = 4;
}
