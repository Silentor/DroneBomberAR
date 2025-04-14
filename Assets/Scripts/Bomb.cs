using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace Silentor.Bomber
{
    public class Bomb : MonoBehaviour
    {
        public GameObject ExplosionPrefab;

        public float FlyDistance { get; private set; }

        public void Init( Vector3 startVelocity, IReadOnlyList<Ground> grounds )
        {
            _velocity = startVelocity;
            var startPosition = transform.position;
            _oldPosition = startPosition;
            _radius = GetComponent<SphereCollider>().radius;

            //Try to find tank on grenade trajectory to find the proper ground
            float highestGround = float.MinValue;
            var hits = Physics.SphereCastAll( transform.position, 0.15f, Vector3.down, 2, LayersMask.Interactables );
            foreach ( var hit in hits )
            {
                if ( hit.rigidbody.gameObject.GetComponent<Tank>() )
                {
                    if( highestGround < hit.transform.position.y )
                    {
                        highestGround = hit.transform.position.y;
                    }
                }
            }

            //Try to find the ground under the grenade
            if( Mathf.Approximately( highestGround, float.MinValue ) )
            {
                foreach ( var ground in grounds )
                {
                    if( ground.IsPointInGround( startPosition ) )
                        if( ground.Plane.center.y > highestGround )
                            highestGround = ground.Plane.center.y;
                }    
            }

            if( highestGround > float.MinValue )
            {
                _explodeHeight = highestGround;
            }
            else
            {
                _explodeHeight = transform.position.y - 2;
            }
        }


        private Vector3 _velocity;
        private const float Gravity = -9.81f;
        private float _explodeHeight;
        private Vector3 _oldPosition;
        private float _radius;

        // Update is called once per frame
        void Update()
        {
            _oldPosition = transform.position;
            _velocity += new Vector3( 0, Gravity * Time.deltaTime, 0 );
            transform.position += _velocity * Time.deltaTime;
            var fallVector = transform.position - _oldPosition;
            var fallStepMagnitude = fallVector.magnitude;
            FlyDistance += fallStepMagnitude;

            var hits = ArrayPool<RaycastHit>.Shared.Rent( 16 );
            try
            {
                var hitsCount = Physics.SphereCastNonAlloc( _oldPosition, _radius, fallVector.normalized, hits, fallStepMagnitude, LayersMask.Interactables );
                for ( int i = 0; i < hitsCount; i++ )
                {
                    var hit =           hits[i];
                    if ( hit.rigidbody.TryGetComponent( out Tank tank ) )
                    {
                        transform.position = hit.point;
                        tank.Damage( this );
                        Explode();
                        return;
                    } 
                }
            }
            finally
            {
                ArrayPool<RaycastHit>.Shared.Return( hits );
            }

            if( transform.position.y < _explodeHeight )
                Explode();
        }

        private void Explode( )
        {
            var explosion = Instantiate( ExplosionPrefab, transform.position, Quaternion.identity );
            Destroy( gameObject );
        }
    }
}
