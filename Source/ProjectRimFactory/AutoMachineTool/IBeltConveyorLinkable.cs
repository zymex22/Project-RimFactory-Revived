using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;

namespace ProjectRimFactory.AutoMachineTool
{
    interface IBeltConveyorLinkable : ProjectRimFactory.Common.IPRF_Building
    {
        void Link(IBeltConveyorLinkable linkable);
        void Unlink(IBeltConveyorLinkable linkable);
        Rot4 Rotation { get; }
        IntVec3 Position { get; }
        bool ReceivableNow(bool underground, Thing thing);


        bool CanSendToLevel(ConveyorLevel level);
        bool CanReceiveFromLevel(ConveyorLevel level);

        // Can the BCL link TO another (irregardless of whether the other can
        //     link from this one)
        bool CanLinkTo(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition=true);
        // can the BLC take a link FROM another (independent of whether the other
        //     can actually link to this one)
        bool CanLinkFrom(IBeltConveyorLinkable otherBeltLinkable, bool checkPosition = true);
        // If both A.CanLinkTo(B) and B.CanLinkFrom(A) then it's a link!

        bool IsUnderground { get; }
        IEnumerable<Rot4> OutputRots { get; }
        bool IsStuck { get; }
    }
    public enum ConveyorLevel {
        Underground=-1,
        Ground=0,
    }
}
