namespace ProjectRimFactory.Common;

internal interface IProductionSettingsUser
{
    PRFBSetting SettingsOptions { get; }
    bool ObeysStorageFilters { get; set; }
    bool OutputToEntireStockpile { get; set; }
        
}