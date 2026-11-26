using UnityEngine;
using Verse;

namespace PickMeUp
{
    [StaticConstructorOnStartup]
    public static class PassionTextures
    {
        // RimWorld's own passion icons: the grey minor flame for "no passion", then minor and major.
        public static readonly Texture2D None = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinorGray");
        public static readonly Texture2D Minor = ContentFinder<Texture2D>.Get("UI/Icons/PassionMinor");
        public static readonly Texture2D Major = ContentFinder<Texture2D>.Get("UI/Icons/PassionMajor");
    }
}
