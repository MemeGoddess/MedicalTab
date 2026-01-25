// Karel Kroeze
// MainTabWindow_Medical.cs
// 2017-05-14

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Fluffy {
    public enum SourceType {
        Colonists,
        Animals,
        Prisoners,
        Visitors,
        Hostiles
    }

    public class MainTabWindow_Medical: MainTabWindow_PawnTable {
        #region Fields

        private static readonly FieldInfo _tableFieldInfo;
        private static bool _filterHealthy;
        private SourceType _source = SourceType.Colonists;

        #endregion Fields

        #region Constructors

        static MainTabWindow_Medical() {
            _tableFieldInfo = typeof(MainTabWindow_PawnTable).GetField("table",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (_tableFieldInfo == null) {
                throw new NullReferenceException("table field not found!");
            }
        }

        public MainTabWindow_Medical() {
            Instance = this;
        }

        #endregion Constructors

        #region Properties

        public static MainTabWindow_Medical Instance { get; private set; }

        public SourceType Source {
            get => _source;
            private set {
                _source = value;
                RebuildTable();
            }
        }

        public PawnTable Table {
            get => _tableFieldInfo.GetValue(this) as PawnTable_PlayerPawns;
            private set => _tableFieldInfo.SetValue(this, value);
        }

        public static bool FilterHealthy {
            get => _filterHealthy;
            set {
                if (_filterHealthy == value) {
                    return;
                }

                _filterHealthy = value;
                Instance.RebuildTable();
            }
        }

        protected override IEnumerable<Pawn> Pawns {
            get
            {
                var pawns = Source switch
                {
                    SourceType.Colonists => Find.CurrentMap.mapPawns.FreeColonists,

                    SourceType.Animals => Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                        .Where(p => p.RaceProps.Animal)
                        .OrderByDescending(p => p.RaceProps.petness)
                        .ThenBy(p => p.RaceProps.baseBodySize)
                        .ThenBy(p => p.def.label),

                    SourceType.Prisoners => Find.CurrentMap.mapPawns.PrisonersOfColony,

                    SourceType.Hostiles => Find.CurrentMap.mapPawns.AllPawnsSpawned.Where(p =>
                        p.RaceProps.Humanlike && p.Faction.HostileTo(Faction.OfPlayer) &&
                        (Settings.ShowAllHostiles || p.health.Downed) &&
                        !Find.CurrentMap.fogGrid.IsFogged(p.PositionHeld)),

                    SourceType.Visitors => Find.CurrentMap.mapPawns.AllPawnsSpawned.Where(p =>
                        p.RaceProps.Humanlike && p.Faction != Faction.OfPlayer &&
                        !p.Faction.HostileTo(Faction.OfPlayer) && !Find.CurrentMap.fogGrid.IsFogged(p.PositionHeld)),
                    _ => base.Pawns
                };

                if (_filterHealthy) {
                    pawns = pawns.Where(p => !p.IsHealthy());
                }

                return pawns;
            }
        }

        protected override PawnTableDef PawnTableDef => DynamicPawnTableDefOf.Medical;

        protected override float ExtraBottomSpace => base.ExtraBottomSpace - 20f;

        #endregion Properties

        #region Methods

        public void DoSourceSelectionButton(Rect rect) {
            // apparently, font size going to tiny on fully zooming in is working as designed...
            Text.Font = GameFont.Small;
            if (!Widgets.ButtonText(rect, Source.ToString().Translate())) return;


            var options = Enum.GetValues(typeof(SourceType))
                .OfType<SourceType>()
                .Select(sourceOption=> new FloatMenuOption(sourceOption.ToString().Translate(),
                    delegate { Source = sourceOption; }))
                .ToList();

            Find.WindowStack.Add(new FloatMenu(options));
        }

        public void DoFilterHealthyButton(Rect rect) {
            TooltipHandler.TipRegion(rect,
                FilterHealthy ? "MedicalTab.FilterHealthyOn".Translate() : "MedicalTab.FilterHealthyOff".Translate());
            if (Widgets.ButtonImage(rect, FilterHealthy ? Resources.HealthyFilterOn : Resources.HealthyFilterOff, Color.white,
                GenUI.MouseoverColor)) {
                FilterHealthy = !FilterHealthy;
            }
        }

        public override void DoWindowContents(Rect rect)
        {

            var sourceRect = rect.BottomPartPixels(30f).LeftPartPixels(120f);
            DoSourceSelectionButton(new Rect(sourceRect.x, sourceRect.y, 120f, 30f));
            DoFilterHealthyButton(new Rect(sourceRect.x + 120f + Margin, sourceRect.y, 30f, 30f));

            // Gotta move stuff down to not overlap button.
            //rect.y += 20;
            base.DoWindowContents(rect);

            
        }

        private void RebuildTable() {
            DynamicPawnTableDefOf.Medical.Select(c => (c.Worker as IOptionalColumn)?.ShowFor(Source) ?? true);
            Table = new PawnTable_PlayerPawns(DynamicPawnTableDefOf.Medical, () => Pawns, UI.screenWidth - (int) (Margin * 2f), (int) (UI.screenHeight - 35 - ExtraBottomSpace - ExtraTopSpace - (Margin * 2f)));
            Table.SetDirty();
            Notify_ResolutionChanged();
        }

        #endregion Methods
    }
}
