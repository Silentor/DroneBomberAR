using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Silentor.Bomber
{
    public class Ground
    {
        public TrackableId Id => Plane.trackableId;
        public readonly ARPlane Plane;
        public ARAnchor Anchor;
        public Rect PlaneRect { get; private set; }                     //World plane 2D AABB

        public Ground(ARPlane plane )
        {
            Plane = plane;
            Plane.boundaryChanged += PlaneOnboundaryChanged;
        }

        private void PlaneOnboundaryChanged( ARPlaneBoundaryChangedEventArgs args )
        {
            //Update plane rect
            PlaneRect = new Rect( Plane.center.ToVector2XZ() - Plane.extents, Plane.size );
        }

        public Vector3? GetRandomPointOnGround( )
        {
            if ( !Plane )               //Plane is destroyed, so ground will be discarded soon
                return null;

            var planeCenter = Plane.center;
            var planeSize   = Plane.size;
            var minX = planeCenter.x - planeSize.x / 2;
            var maxX = planeCenter.x + planeSize.x / 2;
            var minZ = planeCenter.z - planeSize.y / 2;
            var maxZ = planeCenter.z + planeSize.y / 2;

            for ( int tries = 0; tries < 10; tries++ )
            {
                var randomX = UnityEngine.Random.Range( minX, maxX );
                var randomZ = UnityEngine.Random.Range( minZ, maxZ);
                var y      = planeCenter.y;
                var randomPoint = new Vector3( randomX, y, randomZ );
                if ( IsPointInGround( randomPoint ) )
                    return randomPoint;
            }

            return null;
        }

        public Boolean IsPointInGround( Vector3 worldPoint )
        {
            var localPoint = Plane.transform.InverseTransformPoint( worldPoint ).ToVector2XZ();    //Ignore Y axis

            //Fast check
            var planeBounds = new Rect( Plane.centerInPlaneSpace - Plane.size / 2, Plane.size );
            if( !planeBounds.Contains( localPoint ) )
                return false;

            //Convex hull check
            //todo Burst it
            //Debug.Log( $"[{nameof(Ground)}]-[{nameof(IsPointInGround)}] boundary {String.Join( ", ", Plane.boundary.ToArray())}" );
            var localHull = Plane.boundary;
            for ( int i = 0; i < localHull.Length; i++ )
            {
                var p1 = localHull[i];
                var p2 = localHull[(i + 1) % localHull.Length];
                //Debug.DrawLine( p1, p2, Color.red, 1f );

                //Check if point is in the convex hull
                var edge    = p2         - p1;
                var toPoint = localPoint - p1;
                if ((edge.x * toPoint.y - edge.y * toPoint.x) > 0)       //2D "cross product"
                    return false;
            }

            return true;
        }
    }
}