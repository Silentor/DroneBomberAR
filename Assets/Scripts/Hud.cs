using TMPro;
using UnityEngine;
using Object = System.Object;

namespace Silentor.Bomber
{
    public class Hud : MonoBehaviour
    {
        public TMP_Text TanksCount;
        public TMP_Text GrounsCount;
        private GameLogic _gameplay;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
             _gameplay = FindAnyObjectByType<GameLogic>();
        }

        // Update is called once per frame
        void Update()
        {
            if ( TanksCount.gameObject.activeInHierarchy )
            {
                  TanksCount.text = $"Tanks: {_gameplay.Tanks.Count}";
                  GrounsCount.text = $"Grounds: {_gameplay.Grounds.Count}";
            }        
        }
    }
}
