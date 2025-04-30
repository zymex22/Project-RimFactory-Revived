namespace ProjectRimFactory.Common
{
    internal interface IPickupSettings
    {

        bool AllowGroundPickup { get; set; }
        bool AllowStockpilePickup { get; set; }
        bool AllowStoragePickup { get; set; }
        bool AllowForbiddenPickup { get; set; }
        bool AllowBeltPickup { get; set; }


    }
}
