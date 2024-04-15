using RimWorld;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_RenameColdStorage : Dialog_Rename<Building_ColdStorage>
    {

        public Dialog_RenameColdStorage(Building_ColdStorage renaming) : base(renaming)
        {
            // TODO Check if we need that line
            curName = ((IRenameable)renaming).RenamableLabel;
        }

        protected override void OnRenamed(string name)
        {
            base.OnRenamed(name);
            // TODO Check if we still need to set that
            //building.UniqueName = curName;
            Messages.Message("PRFStorageBuildingGainsName".Translate(curName), MessageTypeDefOf.TaskCompletion);
        }
    }
}

