using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    public interface IBeltConveyorLinkable : ProjectRimFactory.Common.IPRF_Building, ILoadReferenceable
    {
        void Link(IBeltConveyorLinkable linkable);
        void Unlink(IBeltConveyorLinkable linkable);
        Rot4 Rotation { get; }
        IntVec3 Position { get; }
        bool Spawned { get; }
        // Because we are using a central Placing mechanism, it makes
        //   more sense to try directions specifically? Maybe??
        //        bool ReceivableNow(bool underground, Thing thing);

        bool CanSendToLevel(ConveyorLevel level);
        bool CanReceiveFromLevel(ConveyorLevel level);

        /// <summary>
        /// Whether the conveyor linkable can accpet thing *right this moment*
        /// </summary>
        bool CanAcceptNow(Thing t);

        /// <summary>
        /// Can the BeltConveyorLinkable link TO another (regardless of whether
        /// the other can link from this one)
        /// </summary>
        /// <param name="checkPosition">If set to <c>false</c>, assume position is valid, 
        /// and only check other considerations - probably only used internally.</param>
        bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true);
        // can the BLC take a link FROM another (independent of whether the other
        //     can actually link to this one)
        /// <summary>
        /// Can the BeltConveyorLinkable link FROM another (independent of whether
        ///   the other can actually link to this one)
        /// </summary>
        /// <param name="checkPosition">If set to <c>false</c>, assume position is valid, 
        /// and only check other considerations - probably only used internally.</param>
        bool CanLinkFrom(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true);
        // If both A.CanLinkTo(B) and B.CanLinkFrom(A) then it's a link!
        // if either A->B or B->A, then they have A link.
        /// <summary>
        /// Has a link with otherBelt, whether to or from
        /// </summary>
        bool HasLinkWith(IBeltConveyorLinkable otherBelt);
        /// <summary>
        /// Get a list of output directions the beltlinkable can actually send to
        ///   at the moment.  Used for drawing output directional arrows!
        /// </summary>
        IEnumerable<Rot4> ActiveOutputDirections { get; }

        bool IsUnderground { get; }
//        IEnumerable<Rot4> OutputRots { get; }
        bool IsStuck { get; }
    }
    public enum ConveyorLevel {
        Underground=-1,
        Ground=0,
    }
}
