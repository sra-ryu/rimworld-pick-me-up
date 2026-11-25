using UnityEngine;
using Verse;

namespace PickMeUp
{
    [StaticConstructorOnStartup]
    public static class ArrowTextures
    {
        // Textures/PickMeUp/Arrow.png
        public static readonly Texture2D Arrow = ContentFinder<Texture2D>.Get("PickMeUp/Arrow");
    }
}
