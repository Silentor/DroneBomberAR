using UnityEngine;

namespace Silentor.Bomber
{
    public static class Layers
    {
        public static readonly int Interactables = LayerMask.NameToLayer( "Interactables" );
    }

    public static class LayersMask
    {
        public static readonly int Interactables = 1 << Layers.Interactables;
    }
}