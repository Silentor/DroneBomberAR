using Unity.Mathematics;
using UnityEngine;

namespace Silentor.Bomber
{
    public class Drone : MonoBehaviour
    {
        private Camera _droneCamera;
        private AudioSource _audio;

        private Vector3 _oldPosition;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _audio = GetComponent<AudioSource>();
            _droneCamera = GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            var velo = (transform.position - _oldPosition) / Time.deltaTime;
            _oldPosition = transform.position;

            var targetPitch = 1d;
            targetPitch *= math.remap( 0d, -0.5, 1, 0.65, velo.y );      //Low pitch when going down
            velo = new Vector3( velo.x, math.max( 0, velo.y ), velo.z );        //Disable negative Y velocity because we already processed it
            targetPitch *= math.remap( 0d, 1, 1, 3, velo.magnitude );
            targetPitch = math.clamp( targetPitch, 0.75, 3 );
            var actualPitch = _audio.pitch;
            actualPitch = Mathf.MoveTowards( actualPitch, (float)targetPitch, 2f * Time.deltaTime );
            _audio.pitch = actualPitch;

            //Debug.Log( actualPitch );
        }

    }
}
