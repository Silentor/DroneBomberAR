using TMPro;
using UnityEngine;

namespace Silentor.Bomber
{
    public class Hud : MonoBehaviour
    {
        public TMP_Text TanksCount;
        public TMP_Text GrounsCount;
        public TMP_Text Velocity;
        private GameLogic _gameplay;
        private Drone _drone;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Application.targetFrameRate = 30;
             _gameplay = FindAnyObjectByType<GameLogic>();
             _drone = FindAnyObjectByType<Drone>();
        }

        // Update is called once per frame
        void Update()
        {
            if ( TanksCount.gameObject.activeInHierarchy )
            {
                  TanksCount.text = $"Tanks: {_gameplay.Tanks.Count}";
                  GrounsCount.text = $"Grounds: {_gameplay.Grounds.Count}";
                  Velocity.text = $"Velocity: {_drone.Velocity} m/s";
            }        
        }
    }
}
