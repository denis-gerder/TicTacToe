using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Scripts
{
    public class AIConfigurator : MonoBehaviour
    {
        [SerializeField]
        private AIConfigSO _aIConfigSO;

        [SerializeField]
        private GameObject _visibleScreen;

        private void Awake()
        {
            if (!_aIConfigSO.AIConfigurated)
            {
                ConfigureAI();
                _aIConfigSO.AIConfigurated = true;
            }

            gameObject.SetActive(false);
            _visibleScreen.SetActive(true);
        }

        public void ConfigureAI() { }
    }
}
