using UnityEngine;

namespace TicTacToe
{
    public class PlayerSymbolRotator : MonoBehaviour
    {
        [Range(0, 100)]
        public int RotationSpeed;

        private void Update()
        {
            transform.Rotate(0, 0, RotationSpeed * Time.deltaTime);
        }
    }
}
