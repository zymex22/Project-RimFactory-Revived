using ProjectRimFactory.AutoMachineTool;
using Verse;
namespace ProjectRimFactory
{
    /// <summary>
    /// This class is specifically so that Conveyors can be notified immediately
    ///    when something takes their carried item (from their thingOwner)
    /// I have no idea if there is a better way to do it.  This is how I am doing it.
    ///   For now.
    /// </summary>
    public class ThingOwner_Conveyor : ThingOwner<Thing>
    {
        // Apparently I have to do this, else C# cannot figure out to use the
        //   base constructor??
        public ThingOwner_Conveyor(IThingHolder owner) : base(owner)
        {
        }
        public override bool Remove(Thing item)
        {
            Building_BeltConveyor belt;
            if (item.holdingOwner == this) belt = owner as Building_BeltConveyor;
            else return base.Remove(item);
            var result = base.Remove(item);
            if (result)
            {
                belt.Notify_LostItem(item);
            }
            return result;
        }
    }
}
