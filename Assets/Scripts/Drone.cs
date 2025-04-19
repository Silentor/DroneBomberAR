using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Silentor.Bomber
{
    public class Drone : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }

        private Camera _droneCamera;
        private AudioSource _audio;
        private const int VelocityFilterSize = 10;
        private readonly List<Vector3> _velocityFilter = new (VelocityFilterSize);

        private Vector3 _oldPosition;


        void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _droneCamera = GetComponent<Camera>();
        }

        public void EnableDrone( )
        {
            _audio.Play();
        }

        // Update is called once per frame
        void Update()
        {
            var velo = (transform.position - _oldPosition) / Time.deltaTime;
            _oldPosition = transform.position;

            if( _velocityFilter.Count == VelocityFilterSize )                
                _velocityFilter.RemoveAt( 0 );
            _velocityFilter.Add( velo );
            var smoothVelo = Vector3.zero;
            for ( int i = 0; i < _velocityFilter.Count; i++ )                
                smoothVelo += _velocityFilter[i];
            smoothVelo /= _velocityFilter.Count;
            Velocity = smoothVelo;

            var targetPitch = 1d;
            targetPitch *= math.remap( 0d, -1, 1, 0.65, smoothVelo.y );      //Low pitch when going down
            smoothVelo = new Vector3( smoothVelo.x, math.max( 0, smoothVelo.y ), smoothVelo.z );        //Disable negative Y velocity because we already processed it
            targetPitch *= math.remap( 0d, 1.5, 1, 3, smoothVelo.magnitude );
            targetPitch = math.clamp( targetPitch, 0.65, 3 );
            _audio.pitch = (float)targetPitch;

            //Debug.Log( actualPitch );
        }

    }
}
