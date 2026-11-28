using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy {
    // Shows the work priority of def.workType, using the same box as the work tab: a checkbox, or a number with manual priorities on.
    public class PawnColumnWorker_BedRest: PawnColumnWorker, IOptionalColumn {
        public override void DoCell(Rect rect, Pawn pawn, PawnTable table) {
            if (pawn.Dead || pawn.workSettings == null || !pawn.workSettings.EverWork) {
                return;
            }

            float x = rect.x + (rect.width - WidgetsWork.WorkBoxSize) / 2f;
            float y = rect.y + (rect.height - WidgetsWork.WorkBoxSize) / 2f;

            Text.Font = GameFont.Medium;
            WidgetsWork.DrawWorkBoxFor(x, y, pawn, def.workType, false);
            Text.Font = GameFont.Small;

            Rect box = new Rect(x, y, WidgetsWork.WorkBoxSize, WidgetsWork.WorkBoxSize);
            if (Mouse.IsOver(box)) {
                TooltipHandler.TipRegion(box, () => WidgetsWork.TipForPawnWorker(pawn, def.workType, false),
                    pawn.thingIDNumber ^ def.workType.GetHashCode());
            }
        }

        public override int Compare(Pawn a, Pawn b) {
            return GetValueToCompare(a).CompareTo(GetValueToCompare(b));
        }

        public bool ShowFor(SourceType source) {
            return source == SourceType.Colonists;
        }

        protected override string GetHeaderTip(PawnTable table) {
            return def.workType.description + "\n\n" + "ClickToSortByThisColumn".Translate();
        }

        private int GetValueToCompare(Pawn pawn) {
            if (pawn.workSettings == null || !pawn.workSettings.EverWork || pawn.WorkTypeIsDisabled(def.workType)) {
                return -1;
            }

            // priority 1 is the highest, and 0 means off
            int priority = pawn.workSettings.GetPriority(def.workType);
            return priority == 0 ? 0 : 5 - priority;
        }
    }
}
