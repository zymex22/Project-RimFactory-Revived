using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common;

public interface IPRF_SettingsContent
{
    float ITab_Settings_Minimum_x { get; }
    float ITab_Settings_Additional_y { get; }

    //may need to pass some pos context
    Listing_Standard ITab_Settings_AppendContent(Listing_Standard list, Rect parrent_rect);



}