using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PickMeUp
{
    /// <summary>
    /// Draws an arrow above every hostile pawn on this map that matches the skill conditions.
    /// RimWorld creates one instance per map on its own, so no Def or patch is needed.
    /// </summary>
    public class ArrowMarkerMapComponent : MapComponent
    {
        // How often the matches are recomputed. Real time, so it also works while the game is paused.
        private const float RefreshInterval = 0.25f;

        // Arrow size in map cells, and how far above the pawn's centre it floats (also in cells).
        private const float ArrowSizeInCells = 0.8f;
        private const float ArrowOffsetInCells = 1.2f;

        private readonly List<Pawn> matches = new List<Pawn>();

        // thingIDNumber of the pawns whose arrow the player clicked away. Saved with the game.
        private readonly HashSet<int> hiddenPawnIds = new HashSet<int>();
        private List<int> hiddenSaveBuffer;
        private float nextRefreshTime;

        public ArrowMarkerMapComponent(Map map) : base(map)
        {
        }

        public int HiddenCount => hiddenPawnIds.Count;

        public bool IsHidden(Pawn pawn)
        {
            return hiddenPawnIds.Contains(pawn.thingIDNumber);
        }

        public void SetHidden(Pawn pawn, bool hidden)
        {
            if (hidden)
            {
                hiddenPawnIds.Add(pawn.thingIDNumber);
            }
            else
            {
                hiddenPawnIds.Remove(pawn.thingIDNumber);
            }
        }

        /// <summary>Brings back every arrow the player clicked away.</summary>
        public void ShowAllHidden()
        {
            hiddenPawnIds.Clear();
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                hiddenSaveBuffer = new List<int>(hiddenPawnIds);
            }
            Scribe_Collections.Look(ref hiddenSaveBuffer, "hiddenPawnIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                hiddenPawnIds.Clear();
                if (hiddenSaveBuffer != null)
                {
                    hiddenPawnIds.UnionWith(hiddenSaveBuffer);
                }
            }
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                hiddenSaveBuffer = null;
            }
        }

        public override void MapComponentOnGUI()
        {
            // Mouse events are needed too: clicking an arrow hides it.
            EventType type = Event.current.type;
            if ((type != EventType.Repaint && type != EventType.MouseDown && type != EventType.MouseUp)
                || map != Find.CurrentMap || !PickMeUpMod.Settings.enabled)
            {
                return;
            }

            if (type == EventType.Repaint && Time.realtimeSinceStartup >= nextRefreshTime)
            {
                nextRefreshTime = Time.realtimeSinceStartup + RefreshInterval;
                Refresh();
            }

            if (matches.Count == 0)
            {
                return;
            }

            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(2);
            float size = Find.CameraDriver.CellSizePixels * ArrowSizeInCells;

            foreach (Pawn pawn in matches)
            {
                // The list can be up to RefreshInterval old: skip pawns that left or died since.
                if (!pawn.Spawned || pawn.Dead || pawn.Map != map || !view.Contains(pawn.Position)
                    || IsHidden(pawn))
                {
                    continue;
                }

                Vector2 pos = GenMapUI.LabelDrawPosFor(pawn, ArrowOffsetInCells);
                var rect = new Rect(pos.x - size / 2f, pos.y - size / 2f, size, size);
                GUI.DrawTexture(rect, ArrowTextures.Arrow);

                // A window on top of the arrow gets the click instead.
                if (Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) != null)
                {
                    continue;
                }
                Widgets.DrawHighlightIfMouseover(rect);
                TooltipHandler.TipRegion(rect, "PMU_HideArrowTip".Translate());
                if (Widgets.ButtonInvisible(rect, doMouseoverSound: false))
                {
                    SetHidden(pawn, true);
                }
            }
        }

        private void Refresh()
        {
            matches.Clear();
            matches.AddRange(PawnMatcher.FindMatches(map));

            // Forget hidden pawns that are gone (dead, left, captured), so the list cannot grow forever.
            if (hiddenPawnIds.Count > 0)
            {
                hiddenPawnIds.RemoveWhere(id => !map.mapPawns.AllPawnsSpawned.Any(p => p.thingIDNumber == id));
            }
        }
    }
}
