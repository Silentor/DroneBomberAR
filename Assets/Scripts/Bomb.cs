using System.Collections.Generic;
using UnityEngine;

namespace Silentor.Bomber
{
    public class Bomb : MonoBehaviour
    {
        public GameObject ExplosionPrefab;

        public void Init( Vector3 startVelocity, IReadOnlyList<Ground> grounds )
        {
            _velocity = startVelocity;

            //Detect ground level to explode
            var startPosition = transform.position;
            var highestGround = float.MinValue;
            foreach ( var ground in grounds )
            {
                if( ground.IsPointInGround( startPosition ) )
                    if( ground.Plane.center.y > highestGround )
                        highestGround = ground.Plane.center.y;
            }

            if( highestGround > float.MinValue )
            {
                _explodeHeight = highestGround;
            }
            else
            {
                _explodeHeight = startPosition.y - 2;
            }
        }


        private Vector3 _velocity;
        private const float Gravity = -9.81f;
        private float _explodeHeight;

        // Update is called once per frame
        void Update()
        {
            _velocity += new Vector3( 0, Gravity * Time.deltaTime, 0 );
            transform.position += _velocity * Time.deltaTime;

            if( transform.position.y < _explodeHeight )
                Explode();
        }

        private void OnTriggerEnter( Collider other )
        {
            if ( other.TryGetComponent( out Tank tank ) )
            {
                tank.Damage();
                Explode();
                
            }
        }

        private void Explode( )
        {
            var explosion = Instantiate( ExplosionPrefab, transform.position, Quaternion.identity );
            Destroy( gameObject );
        }
    }
}
