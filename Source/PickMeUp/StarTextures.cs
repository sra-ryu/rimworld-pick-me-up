using UnityEngine;
using Verse;

namespace PickMeUp
{
    [StaticConstructorOnStartup]
    public static class StarTextures
    {
        // Textures/PickMeUp/Star.png
        public static readonly Texture2D Star = ContentFinder<Texture2D>.Get("PickMeUp/Star");
    }
}
