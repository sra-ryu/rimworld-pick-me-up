using Verse;

namespace PickMeUp
{
    // Entry point: RimWorld instantiates every Mod subclass once at startup.
    public class PickMeUpMod : Mod
    {
        public PickMeUpMod(ModContentPack content) : base(content)
        {
            Log.Message("[Pick Me Up] Loaded.");
        }
    }
}
