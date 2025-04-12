using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Object = System.Object;

namespace Silentor.Bomber
{
    public class Tank : MonoBehaviour
    {
        public float RotationSpeed = 90;
        public float MovementSpeed = 1;
        private Ground _ground;
        private Vector3 _startPosition;
        private GameLogic _game;

        public void Init ( GameLogic game, Ground ground )
        {
            _game = game;
            _ground = ground;
            _startPosition = transform.position;

            Debug.Log( $"[{nameof(Tank)}]-[{nameof(Init)}] tank inited, start AI" );
            Roam( destroyCancellationToken ).Forget(  Debug.LogException );
        }

        private async UniTask Roam( CancellationToken cancel )
        {
            while ( true )
            {
                await    UniTask.Delay( TimeSpan.FromSeconds( 1 ), cancellationToken: cancel );

                Debug.Log( $"[{nameof(Tank)}]-[{nameof(Roam)}] getting point for roam..." );
                CheckGround();
                var dest = _ground?.GetRandomPointOnGround();         
                if ( dest.HasValue )
                {
                    Debug.Log( $"[{nameof(Tank)}]-[{nameof(Roam)}] get point, moving" );
                    await MoveTo( dest.Value, cancel );
                }
            }
        }

        private async UniTask MoveTo( Vector3 destination, CancellationToken cancel )
        {
            var oldGround = _ground;
            while ( true )
            {
                if( oldGround != _ground )          //Tank's ground was changed, its better to stop moving and repeat roaming
                {
                    return;
                }

                var remainPath = destination - transform.position;
                if ( remainPath.magnitude < MovementSpeed * Time.deltaTime )
                {
                    transform.position = destination;
                    return;
                }

                var direction = remainPath.normalized;
                var newPosition = transform.position + direction * MovementSpeed * Time.deltaTime;
                transform.position = newPosition;
                transform.forward = direction;

                await UniTask.NextFrame( cancel );
            }
        }

        /// <summary>
        /// Planes in AR is very volatile, so we need to be aligned with the ground and reconnect to another ground if current one is destroyed
        /// </summary>
        private void CheckGround( )
        {
            if( _ground.Plane )
            {
                if ( _ground.Plane.subsumedBy )
                {
                      _ground = _game.Grounds.FirstOrDefault( x => x.Plane == _ground.Plane.subsumedBy );
                      return;
                }

                if ( _ground.Plane.trackingState >= TrackingState.Limited )
                {
                    if( Mathf.Abs( _ground.Plane.center.y - transform.position.y ) > 0.1f )          //Stick to the ground
                    {
                        transform.position = new Vector3( transform.position.x, _ground.Plane.center.y, transform.position.z );
                    }
                }
            }
            else //Find new ground nearby
            {
                _ground = _game.FindGroundFor( transform.position );
            }
        }
    }
}
