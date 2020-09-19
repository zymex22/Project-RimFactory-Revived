using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public interface IBeltConveyorLinkable : ProjectRimFactory.Common.IPRF_Building, ILoadReferenceable
    {
        /// <summary>
        /// Notify the IBeltConveyorLinkable that <paramref name="linkable"/>
        ///   is a candidate for a link.
        /// </summary>
        /// <param name="linkable">Other linkable.</param>
        void Link(IBeltConveyorLinkable linkable);
        /// <summary>
        /// Notify the IBeltConveyorLinkable that <paramref name="linkable"/>
        ///   will no longer be a valid link (usually despawning)
        /// </summary>
        /// <param name="linkable">Other linkable.</param>
        void Unlink(IBeltConveyorLinkable linkable);

        // Because we are using a central Placing mechanism, it makes
        //   more sense to try directions specifically? Maybe??
        //        bool ReceivableNow(bool underground, Thing thing);

        bool CanSendToLevel(ConveyorLevel level);
        bool CanReceiveFromLevel(ConveyorLevel level);

        /// <summary>
        /// Whether the conveyor linkable can accept thing *right this moment*
        /// </summary>
        bool CanAcceptNow(Thing t);

        /// <summary>
        /// Can the BeltConveyorLinkable link TO another (regardless of whether
        /// the other can link from this one)
        /// </summary>
        /// <param name="checkPosition">If set to <c>false</c>, assume position is valid, 
        /// and only check other considerations - probably only used internally.</param>
        bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true);
        /// <summary>
        /// Can the BeltConveyorLinkable link FROM another (independent of whether
        ///   the other can actually link to this one)
        /// </summary>
        /// <param name="checkPosition">If set to <c>false</c>, assume position is valid, 
        /// and only check other considerations - probably only used internally.</param>
        bool CanLinkFrom(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true);
        // If both A.CanLinkTo(B) and B.CanLinkFrom(A) then it's a link!
        /// <summary>
        /// Has a link with otherBelt, whether to or from
        ///   (either A->B or B->A)
        /// </summary>
        bool HasLinkWith(IBeltConveyorLinkable otherBelt);
        /// <summary>
        /// Get a list of output directions the beltlinkable can actually send to
        ///   at the moment.  Used for drawing output directional arrows!
        /// </summary>
        IEnumerable<Rot4> ActiveOutputDirections { get; }
        /// <summary>
        /// Is the IBCL "underground" - mostly for drawing purposes
        /// </summary>
        bool IsUnderground { get; }
        /// <summary>
        /// Is the conveyor "stuck" - with an item it cannot place
        /// </summary>
        /// <value><c>true</c> if it cannot place the item it has;
        ///   <c>false</c> if it has free space for an item.</value>
        bool IsStuck { get; }
        // // // // RimWorld stuff // // // //
        Rot4 Rotation { get; }
        IntVec3 Position { get; }
        bool Spawned { get; }
    }
    public enum ConveyorLevel {
        Underground=-1,
        Ground=0,
    }
}
