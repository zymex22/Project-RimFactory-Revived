using System;
using Verse;

namespace ProjectRimFactory.Common {
    // ProjectRimFactory is all about producing things, moving things, 
    //   holding things, recycling things, things things things.
    // This interface provides a unified way to move things from one
    //   PRF building to another.
    // Current status: WIP
    public interface IPRF_Building {
        // Thanks to Thornsworth for names

        /// <summary>
        /// Returns true if the IPRF_Building takes responsibility for the <paramref name="newThing"/>
        /// NOTE: If you accept newItem, you should ALWAYS(probably) start with:
        /// <code>if (newItem.Spawned) newItem.DeSpawn();</code>
        /// </summary>
        /// <returns><c>true</c>, if item was accepted, <c>false</c> otherwise.</returns>
        /// <param name="newThing">New item.</param>
        bool AcceptsThing(Thing newThing, IPRF_Building giver = null);
        /// <summary>
        /// Ask the IPRF_Building for an item matching <paramref name="requiredDef"/> 
        ///   with optional Validator, in case the ThingDef is not sufficient to determin
        ///   if the Thing is wanted.
        /// </summary>
        /// <returns>A matching Thing, which is no longer under control of 
        /// the IPRF_Building, or null if no such Things are available.</returns>
        /// <param name="requiredDef">Required def.</param>
        /// <param name="optionalValidator">Optional validator.</param>
        Thing GetThingBy(ThingDef requiredDef, Func<Thing, bool> optionalValidator = null);
        /// <summary>
        /// Should the Building forbid output items?
        /// </summary>
        /// <returns><c>true</c>, if, on placing, thing should be forbidden, <c>false</c> otherwise.</returns>
        bool ForbidOnPlacing();
        // List<Thing> AvailableThings(); // maybe?
        bool ObeysStorageFilters { get; }
        void EffectOnPlaceThing(Thing t);
        void EffectOnAcceptThing(Thing t);



        // some basic building things:
        Map Map { get; }

    }
}
