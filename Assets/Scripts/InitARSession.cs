using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

namespace Silentor.Bomber
{
    public class InitARSession
    {
        public ARSession Session { get; private set; }
        public ARPlaneManager PlaneManager { get; private set; }

        public ARAnchorManager AnchorManager { get; private set; }

        private void LogStateChanged(ARSessionStateChangedEventArgs obj )
        {
            Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(LogStateChanged)}] {obj.state}" );
        }

        public async UniTask<Boolean> Init( CancellationToken cancel )
        {
            Session              =  UnityEngine.Object.FindFirstObjectByType<ARSession>();
            ARSession.stateChanged += LogStateChanged;

            if ((ARSession.state == ARSessionState.None) ||
                (ARSession.state == ARSessionState.CheckingAvailability))
            {
                await ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(Init)}] AR unavailable, stop the app" );
                return false;
            }
            else if ( ARSession.state == ARSessionState.NeedsInstall )
            {
                Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(Init)}] Need install..." );

                await ARSession.Install();

                Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(Init)}] Installed ARCore..." );
            }

            Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(Init)}] before start state {ARSession.state}" );

            await UniTask.WaitUntil( () => ARSession.state > ARSessionState.Ready, cancellationToken: cancel );         //Ready is not fully ready really :) only session ready, but not subsystems

            var descr = Session.descriptor;
            var subs  = Session.subsystem;
            Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(Init)}] Session name {descr.id}, support install {descr.supportsInstall}" );
            Debug.Log( $"session id {subs.sessionId}, requested features {subs.requestedFeatures.ToStringList(  )}, current config {(subs.currentConfiguration != null ? subs.currentConfiguration.Value.features.ToString() : "null")}" );
            var configs = subs.GetConfigurationDescriptors( Allocator.Temp );
            Debug.Log( $"All configurations ({configs.Length}):" );
            foreach ( var config in configs )                    
                Debug.Log( $"{config.identifier}, {config.rank}, {config.capabilities}" );

            //Check camera system
            var cameraResult = InitCamera();
            if ( !cameraResult )
                return false;       //todo process camera permsission

            //Check Plane manager
            var planeManagerResult = InitPlaneManager();
            if( !planeManagerResult )
                return false;

            //Check Anchors
            var anchorResult = InitAnchors();
            if( !anchorResult )
                return false;

            return true;
        }

        private Boolean InitCamera( )
        {
            var cameraSystem = LoaderUtility.GetActiveLoader()?.GetLoadedSubsystem<XRCameraSubsystem>();
            if ( cameraSystem != null )
            {
                if( !cameraSystem.currentCamera.HasFlag( Feature.WorldFacingCamera ))
                {
                    Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(InitCamera)}] World facing camera not supported, nothing to do here. Camera: {cameraSystem.currentCamera.ToString()}" );
                    return false;
                }

                if ( !cameraSystem.permissionGranted )
                {
                    Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(InitCamera)}] camera permission not granted, nothing to do here OR wait for camera permission " );
                    return false;
                }

                Debug.Log( $"Camera permission: {cameraSystem.permissionGranted}, light estim {cameraSystem.currentLightEstimation.ToStringList(  )}" );
                var cameraManager = UnityEngine.Object.FindFirstObjectByType<ARCameraManager>();
                cameraManager.frameReceived += CameraManagerOnFrameReceived;
                Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(InitCamera)}] facing dir {cameraManager.currentFacingDirection}" );
            }
            else
            {
                Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(Init)}] Camera not present, nothing to do here" );
                return false;
            }

            return true;
        }

        private Boolean InitPlaneManager( )
        {
            var planeSystem = LoaderUtility.GetActiveLoader()?.GetLoadedSubsystem<XRPlaneSubsystem>();
            if ( planeSystem != null )
            {
                if ( !planeSystem.currentPlaneDetectionMode.HasFlag( PlaneDetectionMode.Horizontal ) )
                {
                    Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(Init)}] Horizontal plane detection not supported, nothing to do here" );
                    return false;
                }
                Debug.Log( $"Plane system present, h-planes support {planeSystem.subsystemDescriptor.supportsHorizontalPlaneDetection}" );
                PlaneManager = UnityEngine.Object.FindFirstObjectByType<ARPlaneManager>();
                PlaneManager.trackablesChanged.AddListener( PlaneManagerOnPlanesChanged );
            }
            else
            {
                Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(Init)}] Plane system not present, nothing to do here" );
                return false;
            }

            return true;
        }

        private Boolean InitAnchors( )
        {
            if ( LoaderUtility.GetActiveLoader()?.GetLoadedSubsystem<XRAnchorSubsystem>() == null)
            {
                Debug.LogError( $"[{nameof(InitARSession)}]-[{nameof(InitAnchors)}] Anchor subsystem is not present, too bad. It can be workarounded, but not today" );
                return false;
            }

            AnchorManager = UnityEngine.Object.FindFirstObjectByType<ARAnchorManager>();
            return true;
        }

        private void PlaneManagerOnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> planes )
        {
            // if ( planes.added.Count > 0 )
            // {
            //     Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(PlaneManagerOnPlanesChanged)}] added {planes.added.Count} planes" );
            //     foreach ( var addedPlane in planes.added )
            //     {
            //         Debug.Log( $"{addedPlane.trackableId}, align {addedPlane.alignment}" );                    
            //     }
            // }
            //
            // if ( planes.updated.Count > 0 )
            // {
            //     Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(PlaneManagerOnPlanesChanged)}] updated {planes.updated.Count} planes" );
            //     foreach ( var updatedPlane in planes.updated )
            //     {
            //         Debug.Log( $"{updatedPlane.trackableId}, size {updatedPlane.size}" );                    
            //     }
            // }
        }

        private void CameraManagerOnFrameReceived(ARCameraFrameEventArgs frame )
        {
            //Debug.Log( $"[{nameof(InitARSession)}]-[{nameof(CameraManagerOnFrameReceived)}] {frame}" );
        }

        private void OnDestroy( )
        {
            ARSession.stateChanged -= LogStateChanged;
        }
    } 
}
