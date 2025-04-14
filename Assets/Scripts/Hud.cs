using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Silentor.Bomber
{
    public class Hud : MonoBehaviour
    {
        public TMP_Text MissionTime;
        public Image BombIndicator;
        public Color BombReadyColor = Color.yellow;
        public Color BombNotReadyColor = Color.gray;
        public TMP_Text Score;


        [Header("Debug")]
        public GameObject DebugPanel;
        public TMP_Text TanksCount;
        public TMP_Text GrounsCount;
        public TMP_Text Velocity;
        public TMP_Text    Pitch;


        private GameLogic _gameplay;
        private Drone _drone;
        

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Application.targetFrameRate = 30;
             _gameplay = FindAnyObjectByType<GameLogic>();
             _drone = FindAnyObjectByType<Drone>();

#if DEBUG
            DebugPanel.SetActive( true );
#else
            DebugPanel.SetActive( false );
#endif
        }

        // Update is called once per frame
        void Update()
        {
#if DEBUG            
            TanksCount.text  = $"Tanks: {_gameplay.Tanks.Count}";
            GrounsCount.text = $"Grounds: {_gameplay.Grounds.Count}";
            Velocity.text    = $"Velocity: {_drone.Velocity} m/s";
            Pitch.text       = $"Pitch: {_drone.GetComponent<AudioSource>().pitch}";
#endif

            var missionTime = _gameplay.MissionTime;
            MissionTime.text = $"{missionTime.Minutes:D2}:{missionTime.Seconds:D2}";     

            BombIndicator.color = _gameplay.IsBombReady ? BombReadyColor : BombNotReadyColor;
            Score.text = $"Score: {_gameplay.Score}";
        }
    }
}
