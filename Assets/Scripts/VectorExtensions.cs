using UnityEngine;

namespace Silentor.Bomber
{
    public static class VectorExtensions
    {
        public static Vector3 ToVector3XZ( this Vector2 vector2 )
        {
            return new Vector3( vector2.x, 0, vector2.y );
        }

        public static Vector3 ToVector3XZ( this Vector2 vector2, float y )
        {
            return new Vector3( vector2.x, y, vector2.y );
        }

        public static Vector3 ToVector3XZ( this Vector3 vector3 )
        {
            return new Vector3( vector3.x, 0, vector3.z );
        }

        public static Vector2 ToVector2XZ( this Vector3 vector3 )
        {
            return new Vector2( vector3.x, vector3.z );
        }

        public static Vector3 SetY( this Vector3 vector3, float y )
        {
            return new Vector3( vector3.x, y, vector3.z );
        }

    }
}