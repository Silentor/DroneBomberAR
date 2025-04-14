using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace Silentor.Bomber
{
    public class Tank : MonoBehaviour
    {
        public ParticleSystem CombustionFX;
        public GameObject ExplosionFX;
        public ParticleSystem DamagedSmokeFX;
        public ParticleSystem CannonVFX;

        public AudioClip IdleSFX;
        public AudioClip MovingSFX;
        public AudioSource MotorAudioSource;
        public AudioSource CannonSFX;
        

        public float RotationSpeed = 90;
        public float CannonRotationSpeed = 90;
        public float MovementSpeed = 1;

        public Transform CannonTransform;

        private Ground _ground;
        private Vector3 _startPosition;
        private GameLogic _game;
        private int _hitsCount;

        private CancellationTokenSource _deathCancel;

        public void Init ( GameLogic game, Ground ground )
        {
            _game = game;
            _ground = ground;
            _startPosition = transform.position;

            Debug.Log( $"[{nameof(Tank)}]-[{nameof(Init)}] tank inited, start AI" );
            _deathCancel = new CancellationTokenSource();
            var deathCancelToken = CancellationTokenSource.CreateLinkedTokenSource( _deathCancel.Token, destroyCancellationToken ).Token;
            Roam( deathCancelToken ).Forget(   );
            FireCannon( deathCancelToken ).Forget(  );
        }

        public void Damage( )
        {
            if( ++_hitsCount > 1 )
                Explode();
            else
            {
                MovementSpeed /= 2;
                RotationSpeed /= 2;
                CannonRotationSpeed /= 2;
                DamagedSmokeFX.gameObject.SetActive( true );
            }
        }

        public void Explode( )
        {
            //Stop roaming
            _deathCancel.Cancel();

            CombustionFX.Stop();
            DamagedSmokeFX.Stop();

            ExplosionFX.transform.SetParent( null );
            ExplosionFX.gameObject.SetActive( true );

            Destroy( gameObject, 2f );
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
            CombustionFX.Play();
            PlayMovingSFX();

            while ( true )
            {
                if( oldGround != _ground )          //Tank's ground was changed, its better to stop moving and repeat roaming
                {
                    CombustionFX.Stop();
                    PlayIdleSFX();
                    return;
                }

                var remainPath = destination - transform.position;
                if ( remainPath.magnitude < MovementSpeed * Time.deltaTime )
                {
                    PlayIdleSFX();
                    CombustionFX.Stop();
                    transform.position = destination;
                    return;
                }

                var direction = remainPath.normalized;
                while ( (direction - transform.forward).magnitude > 0.01f )
                {
                    transform.forward = Vector3.RotateTowards( transform.forward, direction, RotationSpeed * Mathf.Deg2Rad * Time.deltaTime, 0.0f );
                    await UniTask.NextFrame( cancel );
                }

                var newPosition = transform.position + direction * MovementSpeed * Time.deltaTime;
                transform.position = newPosition;
                transform.forward = direction;

                await UniTask.NextFrame( cancel );
            }
        }

        private async UniTask FireCannon( CancellationToken cancel )
        {
            var defaultRotation = CannonTransform.localRotation.eulerAngles;
            var currentRotation = defaultRotation;

            while ( true )
            {
                //Search target
                await UniTask.Delay( TimeSpan.FromSeconds( Random.Range( 0.1f, 2f ) ), cancellationToken: cancel );

                //Select target direction
                var targetRot = defaultRotation.z + Random.Range( -45f, 45f );

                //Rotate cannon
                while ( !Mathf.Approximately( currentRotation.z, targetRot ) )
                {
                    var rot = currentRotation.z;
                    rot = Mathf.MoveTowards( rot, targetRot, CannonRotationSpeed * Time.deltaTime );
                    currentRotation = new Vector3( currentRotation.x, currentRotation.y, rot );
                    CannonTransform.localRotation = Quaternion.Euler( currentRotation );
                    await UniTask.NextFrame( cancel );
                }

                //Prepare
                await UniTask.Delay( TimeSpan.FromSeconds( Random.Range( 0.5f, 1f ) ), cancellationToken: cancel );

                //Fire
                CannonVFX.Play();
                CannonSFX.Play();
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

        private void PlayMovingSFX( )
        {
            if ( MotorAudioSource.clip != MovingSFX )
            {
                MotorAudioSource.clip = MovingSFX;
                MotorAudioSource.Play();
            }
        }

        private void PlayIdleSFX( )
        {
            if ( MotorAudioSource.clip != IdleSFX )
            {
                MotorAudioSource.clip = IdleSFX;
                MotorAudioSource.Play();
            }
        }
    }
}
