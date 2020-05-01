using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompEffecter : ThingComp
    {
        public CompProperties_Effecter Props => (CompProperties_Effecter)this.props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            this.effectThing = (EffectThing)GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed(EffectThing.DefName), this.parent.Position, this.parent.Map);
            this.effectThing.Init(this.Props?.effect?.Spawn(), this.parent, this.parent);
            this.effectThing.Enable = this.enable;
        }

        private EffectThing effectThing;

        private bool enable = true;

        public bool Enable { get
            {
                return this.enable;
            }
            set
            {
                this.enable = value;
                if(this.effectThing != null)
                {
                    this.effectThing.Enable = value;
                }
            }
        }
    }

    public class CompProperties_Effecter : CompProperties
    {
        public EffecterDef effect;
        
        public CompProperties_Effecter()
        {
            this.compClass = typeof(CompEffecter);
        }
    }

    [StaticConstructorOnStartup]
    public class EffectThing : Thing
    {
        public static string DefName = "PRFEffecterThing";
        static EffectThing()
        {
            ThingDef def = new ThingDef();
            def.defName = DefName;
            def.tickerType = TickerType.Normal;
            def.altitudeLayer = AltitudeLayer.MoteLow;
            def.useHitPoints = false;
            def.isSaveable = false;
            def.rotatable = false;
            def.selectable = false;
            def.thingClass = typeof(EffectThing);
            DefDatabase<ThingDef>.Add(def);
        }

        private TargetInfo a;

        private TargetInfo b;

        private bool enable = true;

        public bool Enable { get => this.enable;
            set
            {
                if(this.enable != value)
                {
                    this.enable = value;
                    if (!value && this.effecter != null)
                    {
                        this.effecter.Cleanup();
                    }
                }
            }
        }

        public void Init(Effecter effecter, TargetInfo a, TargetInfo b)
        {
            this.effecter = effecter;
            this.a = a;
            this.b = b;
        }

        private Effecter effecter;

        public override void Tick()
        {
            if (this.effecter != null)
            {
                if (this.Enable)
                {
                    this.effecter?.EffectTick(this.a, this.b);
                }
            }
        }
    }
}
