using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ProjectRimFactory.Industry
{
    class Building_LogicController : Building
    {
        //List of Values to be used in leaf_Logics
        public List<ValueRefrence> valueRefrences = new List<ValueRefrence>();
        //Comparisions between valueRefrences that yield True or False. To be used in the Algebra of LogicSignals Trees
        public List<Leaf_Logic> leaf_Logics = new List<Leaf_Logic>();
        //Complete Logic Signals
        public List<LogicSignal> LogicSignals = new List<LogicSignal>();


        public void UpdateRegisteredSignals()
        {
            PRFGameComponent pRFGameComponent = Current.Game.GetComponent<PRFGameComponent>();
            pRFGameComponent.LoigSignalRegestry.RemoveAll(i => i.Value == this.Map);
            foreach (LogicSignal valref in LogicSignals)
            {
                pRFGameComponent.LoigSignalRegestry.Add(valref, this.Map);
            }
        }


        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref valueRefrences, "valueRefrences", LookMode.Deep);
            Scribe_Collections.Look(ref leaf_Logics, "leaf_Logics", LookMode.Deep);
            Scribe_Collections.Look(ref LogicSignals, "LogicSignals", LookMode.Deep);

            UpdateRegisteredSignals();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos();
        }

        private void Initialize()
        {
            if (valueRefrences == null)
            {
                valueRefrences = new List<ValueRefrence>();
            }
            //Not really visible to the User Used for Creating New LeafLogic
            valueRefrences.Add(new ValueRefrence_Fixed(0, "Dummy-1", false));
            valueRefrences.Add(new ValueRefrence_Fixed(0, "Dummy-2", false));

            if (leaf_Logics == null)
            {
                leaf_Logics = new List<Leaf_Logic>();
            }
            leaf_Logics.Add(new Leaf_Logic(valueRefrences[0], valueRefrences[1], EnumCompareOperator.Greater, "Dummy-1", false));

            if (LogicSignals == null)
            {
                LogicSignals = new List<LogicSignal>();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            Initialize();

            //Automaticly add a few values for Testing
            valueRefrences.Add(new ValueRefrence_Fixed(5, "SpawnTesting - 1"));
            valueRefrences.Add(new ValueRefrence_Fixed(0, "SpawnTesting - 2"));

            leaf_Logics.Add(new Leaf_Logic(valueRefrences[0], valueRefrences[1], EnumCompareOperator.Greater, "Spawn Test"));

            LogicSignals.Add(new LogicSignal(new Tree(new List<Tree_node> { new Tree_node(EnumBinaryAlgebra.bNA, leaf_Logics[0]), new Tree_node(EnumBinaryAlgebra.bAND, null), new Tree_node(EnumBinaryAlgebra.bNA, leaf_Logics[1]) }), "Logic Testing"));

            UpdateRegisteredSignals();

        }

        public override void Tick()
        {
            base.Tick();
        }
    }







}
