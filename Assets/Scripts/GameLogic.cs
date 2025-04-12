using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = System.Object;

namespace Silentor.Bomber
{
    public class GameLogic : MonoBehaviour
    {
        public Tank TankPrefab;
        public Bomb BombPrefab;
        public Transform TestConvex;

        public IReadOnlyList<Tank> Tanks => _tanks;
        public IReadOnlyList<Ground> Grounds => _grounds;

        public void DropTheBomb( )
        {
             if( Time.time - _lastTimedroppedBomb < 1f )           
                 return;

             _lastTimedroppedBomb = Time.time;
             var newBomb = Instantiate( BombPrefab, _droneCamera.transform.position, Quaternion.identity );
             newBomb.Init( _droneCamera.velocity, Grounds );
        }

        public Ground FindGroundFor( Vector3 position )
        {
            var minHeightDiff = float.MaxValue;
            Ground result = null;

            //Find best overlapping ground
            foreach ( var ground in Grounds )
            {
                if ( ground.IsPointInGround( position ) )
                {
                    var heightDiff = Mathf.Abs( position.y - ground.Plane.center.y );
                    if ( heightDiff < minHeightDiff )
                    {
                        minHeightDiff = heightDiff;
                        result = ground;
                    }
                }
            }

            //Overlapping ground not found, so find the closest one
            if ( result == null )
            {
                var nearestBorderDistance = float.MaxValue;
                foreach ( var ground in Grounds )
                {
                    foreach ( var boundaryPoint in ground.Plane.boundary )
                    {
                        var dist = Vector3.Distance( position, boundaryPoint );
                        if ( dist < nearestBorderDistance )
                        {
                            nearestBorderDistance = dist;
                            result = ground;
                        }
                    }                    
                }
            }

            return result;
        }

        private ARAnchorManager _anchorManager;

        private float _lastTimedroppedBomb = 0f;

        private void Start( )
        {
            _droneCamera = Camera.main;
            GamePlay( destroyCancellationToken ).Forget( Debug.LogException );                            
        }

        private void Update( )
        {
            // if ( TestConvex && Grounds.Any() )
            // {
            //     var pos = TestConvex.position;
            //     if( Grounds[0].IsPointInGround( pos ) )
            //         Debug.Log( "Hit" );
            // }
        }

        private async UniTask GamePlay( CancellationToken cancel )
        {
            var init = new InitARSession();
            var initResult = await init.Init( cancel );
            if ( !initResult )
            {
                Debug.LogError( $"[{nameof(GameLogic)}]-[{nameof(GamePlay)}] init not successfull, game is impossible" );
                return;
            }

            init.PlaneManager.trackablesChanged.AddListener( PlaneManagerOnPlanesChanged );
            _anchorManager = init.AnchorManager;

            SpawnTanks( init.PlaneManager, cancel ).Forget( Debug.LogException );
        }

        private void PlaneManagerOnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> planesChangedEventArgs )
        {
            _newGrounds.Clear();
            _oldGrounds.Clear();

            foreach ( var updatedPlane in planesChangedEventArgs.updated )
            {
                if ( updatedPlane.alignment == PlaneAlignment.HorizontalUp && updatedPlane.size.magnitude > 2f && !updatedPlane.subsumedBy )      //Looks like proper ground
                {
                    _newGrounds.Add( updatedPlane );
                }
                else
                {
                    _oldGrounds.Add( updatedPlane );
                }
            }

            foreach ( var removedPlanes in planesChangedEventArgs.removed )
            {
                _oldGrounds.Add( removedPlanes.Value );
            }

            foreach ( var oldGround in _oldGrounds )
            {
                _grounds.RemoveAll( g => g.Id == oldGround.trackableId );
            }

            foreach ( var newGround in _newGrounds )
            {
                if( !_grounds.Exists( g => g.Id == newGround.trackableId ) )
                    _grounds.Add( new Ground( newGround ) );
            }
        }

        private readonly List<Tank> _tanks = new ();
        private readonly List<Ground> _grounds = new ();
        private readonly List<ARAnchor> _anchors = new ();

        private readonly List<ARPlane> _newGrounds = new ();
        private readonly List<ARPlane> _oldGrounds = new ();
        private Camera _droneCamera;

        private async UniTask SpawnTanks( ARPlaneManager planeManager, CancellationToken cancel )
        {
            //Spawn tanks on big enough horizontal planes
            while ( !cancel.IsCancellationRequested )
            {
                _tanks.RemoveAll( t => !t );            //Cleanup of destroyed tanks

                foreach ( var ground in _grounds )
                {
                    var spawnPos = ground.Plane.transform.position;
                    if ( _tanks.TrueForAll( t => Vector3.Distance( t.transform.position, spawnPos ) > 1 ) )
                    {
                        //var anchor = await GetAnchorForPosition( spawnPos, cancel );
                        //if ( anchor )
                        {
                            //var tank = Instantiate( TankPrefab, spawnPos, Quaternion.identity, anchor.transform );
                            var tank = Instantiate( TankPrefab, spawnPos, Quaternion.identity );
                            _tanks.Add( tank );
                            tank.Init( this, ground );
                            //Debug.Log( $"[{nameof(GameLogic)}]-[{nameof(SpawnTanks)}] Tank spawned on plane {ground.Id}, anchor {anchor.trackableId}" );
                            Debug.Log( $"[{nameof(GameLogic)}]-[{nameof(SpawnTanks)}] Tank spawned on plane {ground.Id}" );
                        }
                        // else
                        // {
                        //     Debug.LogError( $"[{nameof(GameLogic)}]-[{nameof(SpawnTanks)}] anchor not found" );
                        // }
                    }
                }

                await UniTask.Delay( 1000, cancellationToken: cancel );
            }
        }

        private async UniTask<ARAnchor> GetAnchorForPosition( Vector3 position, CancellationToken cancel )
        {
            //Check if there is an anchor close to the position
            foreach ( var anchor in _anchors )
            {
                if ( Vector3.Distance( anchor.transform.position, position ) < 2f )
                {
                    return anchor;
                }
            }

            var newAnchor = await _anchorManager.TryAddAnchorAsync( new Pose( position, Quaternion.identity ) );
            cancel.ThrowIfCancellationRequested();
            if ( newAnchor.status )
            {
                _anchors.Add( newAnchor.value );
                return newAnchor.value;
            }

            return null;
        }
    }
}