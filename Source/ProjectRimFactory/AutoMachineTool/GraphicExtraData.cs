using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;
namespace ProjectRimFactory {
    /****************************************************************
     * Modders can extend defs by use of DefModExtensions. But there
     * are no such option extensions for GraphicData in the XML, and
     * sometimes you just need more data.
     * We can overcome that limitation! GraphicData contains exactly
     * one string: the texPath. We can interpret that as XML and use
     * RimWorld's load-from-xml functions to get the extra data!
     * Win!
     * (Note that we have to encode '<' as '[' Well, anyone who uses
     *  '[' in a filename gets what's coming to them?)
     * USAGE NOTE:
     *   public override void Init(GraphicRequest req) {
     *     var extraData = GraphicExtraData.Extract(req, out this.path, out req2, TODO);
     *     // req2 is new GraphicRequest
     *     // req2.path and req2.graphicData.texPath are correct
     *     // use req2 for init, do all init in ExtraInit:
     *     ExtraInit(req2, extraData); // extraData may be null
     *   }
     * A parent can initialize its children by modifying `req` as
     * above and then calling child.Init(req2); followed by
     * child.ExtraInit(req2, extraData);
     * If the child doesn't do anything in Init() (preferred), it 
     * can simply call child.ExtraInit(req2, extraData);
     *
     * Final note: I still don't fully understand everything I
     * am doing, so ...good luck, god speed, etc?
     ***************************************************************/
    public class GraphicExtraData {
        // If you add something here, consider adding ToString() for it below
        public Vector3? arrowDrawOffset;     // Vector3? so we can
        public Vector3? arrowEastDrawOffset; // test against `null`
        public Vector3? arrowWestDrawOffset; // and only update if
        public Vector3? arrowNorthDrawOffset;  // actually changed
        public Vector3? arrowSouthDrawOffset;  // in def's XML.
        public string texPath = null; // actual texPath
        public string texPath2 = null;  // splitter building, wall edges, whatever?
        public string arrowTexPath1 = null;
        public string arrowTexPath2 = null;
        public GraphicData graphicData1 = null; // wall transitions?
        public List<string> specialLinkDefs;
        public string inputString = null;

        public static GraphicExtraData Extract(GraphicRequest req, 
                                           out GraphicRequest outReq,
                                           bool removeExtraFromReq=false) {
            outReq = CopyGraphicRequest(req);
            if (req.graphicData.texPath[0] == '[') {
                GraphicExtraData extraData = null;
                try {
                    var helperDoc = new System.Xml.XmlDocument();
                    helperDoc.LoadXml(req.graphicData.texPath.Replace('[', '<').Replace(']', '>'));
                    extraData = DirectXmlToObject.ObjectFromXml<GraphicExtraData>(
                                                   helperDoc.DocumentElement, false);
                } catch (Exception e) {
                    Log.Error("GraphicExtraData was unable to extract XML from \"" + req.graphicData.texPath +
                    "\"; Exception: " + e);
                    return null;
                }
                extraData.inputString = req.graphicData.texPath;
                if (removeExtraFromReq) {
                    outReq.graphicData.texPath = extraData.texPath;
                    outReq.path = extraData.texPath;
                }
                Debug.Message(Debug.Flag.ConveyorGraphics, "Graphic Extra Data extracted: " + extraData);
                return extraData;
            }
            #if DEBUG
            else {
                Debug.Message(Debug.Flag.ConveyorGraphics, "Graphic Extra Data empty: " + req.graphicData.texPath);
            }
            #endif
            return null;
        }
        //no idea if this is necessary, but *it works* 
        //  - "it works" is a general theme here
        public static GraphicRequest CopyGraphicRequest(GraphicRequest req, string newTexPath = null) {
            GraphicData gData = new GraphicData();
            gData.CopyFrom(req.graphicData);
            var gr = new GraphicRequest(gData.graphicClass, gData.texPath, req.shader,
                  req.drawSize, req.color, req.colorTwo, gData, req.renderQueue,
                  req.shaderParameters,
                  req.maskPath);
            if (newTexPath != null) {
                gr.path = newTexPath;
                gr.graphicData.texPath = newTexPath;
            }
            return gr;
        }
        #if DEBUG
        // This is only used in graphics debugging:
        public override string ToString() {
            StringBuilder s = new StringBuilder("[ExtraGraphicRequest for \"" + this.inputString + "\": ");
            var pieces = new List<string>();
            foreach (var f in HarmonyLib.AccessTools.GetDeclaredFields(typeof(GraphicExtraData))) {
                if (f.FieldType == typeof(Vector3?)) {
                    var x = f.GetValue(this);
                    if (x != null)
                        pieces.Add(f.Name + ": " + x.ToString());
                    else { } //pieces.Add(" " + f.Name + ": null");
                } else if (f.FieldType == typeof(string) && f.Name!="inputString") {
                    var x = f.GetValue(this);
                    if (x != null)
                        pieces.Add(f.Name + ": \"" + x.ToString() + "\"");
                } else if (f.FieldType == typeof(GraphicData)) {
                    var x = f.GetValue(this);
                    if (x != null)
                        pieces.Add(f.Name + ": [" + x.ToString() + "]");
                }
            }
            if (specialLinkDefs != null) {
                pieces.Add(" specialLinkDefs: [" + String.Join(", ",
                             specialLinkDefs) + "]");
            }
            if (pieces.Count > 0) s.Append(" ").Append(String.Join(", ", pieces));
            s.Append("]");
            return s.ToString();
        }
        #endif
    }
    public interface IHaveGraphicExtraData {
        void ExtraInit(GraphicRequest req, GraphicExtraData extraData);
    }
}
