using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectRimFactory.Common
{
    internal interface IPickupSettings
    {

        bool AllowGroundPickup { get; set; }
        bool AllowStockpilePickup { get; set; }
        bool AllowStoragePickup { get; set; }
        bool AllowForbiddenPickup { get; set; }


    }
}
