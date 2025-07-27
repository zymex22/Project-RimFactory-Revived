using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ProjectRimFactory.Industry
{
    [StaticConstructorOnStartup]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Building_CustomBattery : Building
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksToExplode, "ticksToExplode", 0, false);
        }

        private CompPowerBattery compPowerBattery;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPowerBattery = GetComp<CompPowerBattery>();
        }

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            if (phase != DrawPhase.Draw) return; //Crashes when drawing 2 things at the same time in some of the other phases
            var r = default(GenDraw.FillableBarRequest);
            r.center = DrawPos + Vector3.up * 0.1f + Vector3.forward * 0.25f;
            r.size = BarSize;
            r.fillPercent = compPowerBattery.StoredEnergy / compPowerBattery.Props.storedEnergyMax;
            r.filledMat = BatteryBarFilledMat;
            r.unfilledMat = BatteryBarUnfilledMat;
            r.margin = 0.15f;
            r.rotation = Rotation;
            GenDraw.DrawFillableBar(r);
            if (ticksToExplode > 0 && Spawned)
            {
                Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
            }
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            if (ticksToExplode <= 0) return;
            if (wickSustainer == null)
            {
                StartWickSustainer();
            }
            else
            {
                wickSustainer.Maintain();
            }
            ticksToExplode--;
            if (ticksToExplode != 0) return;
            var randomCell = this.OccupiedRect().RandomCell;
            var radius = Rand.Range(0.5f, 1f) * 3f;
            GenExplosion.DoExplosion(randomCell, Map, radius, DamageDefOf.Flame, null);
            compPowerBattery.DrawPower(400f);
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (!Destroyed && ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && compPowerBattery.StoredEnergy > 500f)
            {
                ticksToExplode = Rand.Range(70, 150);
                StartWickSustainer();
            }
        }

        private void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }

        private int ticksToExplode;

        private Sustainer wickSustainer;

        private static readonly Vector2 BarSize = new(1.25f, 0.35f);

        private static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 1f, 1f));

        private static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f));
    }
}
