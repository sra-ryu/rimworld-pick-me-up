using System.Collections.Generic;
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
        private float nextRefreshTime;

        public ArrowMarkerMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentOnGUI()
        {
            if (Event.current.type != EventType.Repaint || map != Find.CurrentMap
                || !PickMeUpMod.Settings.enabled)
            {
                return;
            }

            if (Time.realtimeSinceStartup >= nextRefreshTime)
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
                if (!pawn.Spawned || pawn.Dead || pawn.Map != map || !view.Contains(pawn.Position))
                {
                    continue;
                }

                Vector2 pos = GenMapUI.LabelDrawPosFor(pawn, ArrowOffsetInCells);
                GUI.DrawTexture(new Rect(pos.x - size / 2f, pos.y - size / 2f, size, size), ArrowTextures.Arrow);
            }
        }

        private void Refresh()
        {
            matches.Clear();
            matches.AddRange(PawnMatcher.FindMatches(map));
        }
    }
}
