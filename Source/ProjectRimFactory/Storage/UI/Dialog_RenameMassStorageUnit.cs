using RimWorld;
using Verse;

namespace ProjectRimFactory.Storage.UI
{
    public class Dialog_RenameMassStorageUnit : Dialog_Rename<Building_MassStorageUnit>
    {

        public Dialog_RenameMassStorageUnit(Building_MassStorageUnit renaming) : base(renaming)
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
