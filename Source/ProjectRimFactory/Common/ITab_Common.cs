using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectRimFactory.Common
{
    static class ITab_Common
    {

        public static GUIStyle richTextStyle
        {
            get
            {
                GUIStyle gtter_richTextStyle = new GUIStyle();
                gtter_richTextStyle.richText = true;
                gtter_richTextStyle.normal.textColor = Color.white;
                return gtter_richTextStyle;
            }
        }



    }
}
