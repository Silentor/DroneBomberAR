using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Silentor.Bomber
{
    public class PlaneVisualizer : MonoBehaviour
    {
        public Canvas PlaneDebugCanvas;
        public Material GroundMat;
        public Material NotGroundMat;
        public Material LimitedGroundMat;

        private ARPlane _plane;
        private TMP_Text _planeSizeText;
        private Camera _mainCamera;
        private GameLogic _gameplay;
        private MeshRenderer _renderer;
        private EGroundType? _currentState;

        void Start()
        {
            _plane = GetComponent<ARPlane>();
            _planeSizeText = PlaneDebugCanvas.transform.Find( "PlaneSize" ).GetComponent<TMP_Text>();
            _mainCamera = Camera.main;
            _gameplay = FindAnyObjectByType<GameLogic>();
            _renderer = GetComponent<MeshRenderer>();
        }

        private void Update( )
        {
            _planeSizeText.text = $"Size {_plane.size}";
            EGroundType state  ;
            if( _gameplay.Grounds.Any( g => g.Id == _plane.trackableId ))
            {
                if ( _plane.trackingState == TrackingState.Tracking )
                    state = EGroundType.Ground;
                else if ( _plane.trackingState == TrackingState.Limited )
                    state = EGroundType.Limited;
                else
                    state = EGroundType.None;
            }
            else
            {
                state = EGroundType.None;
            }

            if ( state != _currentState )
            {
                _currentState = state;
                _renderer.material = state switch 
                {
                    EGroundType.Ground => GroundMat,
                    EGroundType.Limited => LimitedGroundMat,
                    EGroundType.None => NotGroundMat,
                    _ => null
                };
            }
        }

        private void LateUpdate( )
        {
            //Face to camera
            if (_mainCamera != null)
            {
                PlaneDebugCanvas.transform.LookAt(_mainCamera.transform);
                PlaneDebugCanvas.transform.Rotate(0, 180, 0);
            }
        }
    }

    public enum EGroundType
    {
        None,
        Ground,
        Limited
    }
}
