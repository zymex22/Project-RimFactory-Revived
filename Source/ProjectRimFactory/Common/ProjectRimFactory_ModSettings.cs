using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Xml;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModSettings : ModSettings
    {
        private static ContainerRow root;
        private float lastHeight = 1000f;

        private Vector2 scrollPosition;

        public bool RequireReboot => root.RequireReboot;

        public virtual IEnumerable<PatchOperation> Patches => root.GetValidPatches();

        public static void LoadXml(ModContentPack content)
        {
            root = ParseSettingRows(content);
            root.Initialize();
        }

        private static ContainerRow ParseSettingRows(ModContentPack content)
        {
            var r = new ContainerRow();
            var xmlDoc = DirectXmlLoader.XmlAssetsInModFolder(content, "Settings")?.Where(x => x.name == "Settings.xml")
                ?.ToList().FirstOrDefault();
            if (xmlDoc == null || xmlDoc.xmlDoc == null)
            {
                Log.Error("Settings/Settings.xml not found or invalid xml.");
                return r;
            }

            var rootElem = xmlDoc.xmlDoc.DocumentElement;
            if (rootElem.Name != "SettingRows")
            {
                Log.Error("SettingRows not found. name=" + rootElem.Name);
                return r;
            }

            r.Rows.LoadDataFromXmlCustom(rootElem);
            return r;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            root.ExposeData();
            Scribe_Values.Look<Debug.Flag>(ref Debug.activeFlags, "debugFlags");
        }

        public void DoWindowContents(Rect inRect)
        {
            var outRect = new Rect(inRect);
            outRect.yMin += 20f;
            outRect.yMax -= 20f;
            outRect.xMin += 20f;
            outRect.xMax -= 20f;

            var viewRect = new Rect(0f, 0f, outRect.width - 16f, lastHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var list = new Listing_Standard();
            list.Begin(viewRect);
#if DEBUG
            list.Label("Debug Symbols:");
            foreach (var f in (Debug.Flag[]) Enum.GetValues(typeof(Debug.Flag)))
            {
                var ischecked = (f & Debug.activeFlags) > 0;
                list.CheckboxLabeled(f.ToString(), ref ischecked, f.ToString()); // use Desc to force list to highlight
                if (!ischecked == (f & Debug.activeFlags) > 0) Debug.activeFlags ^= f; // toggle f
            }

            list.GapLine();
#endif
            root.Draw(list);
            list.End();
            Widgets.EndScrollView();

            lastHeight = list.CurHeight + 16f;
        }

        public void Apply()
        {
            root.Apply();
        }
    }

    public interface ISettingRow
    {
        bool RequireReboot { get; }
        void Draw(Listing_Standard list);
        void ExposeData();
        void Apply();
        bool Initialize();
        IEnumerable<PatchOperation> GetValidPatches();
    }

    public abstract class SettingRow : ISettingRow
    {
        public bool RequireReboot => false;

        public void Apply()
        {
        }

        public abstract void Draw(Listing_Standard list);

        public void ExposeData()
        {
        }

        public IEnumerable<PatchOperation> GetValidPatches()
        {
            return Enumerable.Empty<PatchOperation>();
        }

        public bool Initialize()
        {
            return true;
        }
    }

    public abstract class SettingItemBase : ISettingRow
    {
        public string description;
        public string key;
        public string label;
        public virtual bool RequireReboot { get; protected set; }

        public abstract void Draw(Listing_Standard list);

        public abstract void ExposeData();

        public abstract void Apply();

        public virtual bool Initialize()
        {
            return true;
        }

        public abstract IEnumerable<PatchOperation> GetValidPatches();
    }

    public abstract class SettingItem : SettingItemBase
    {
        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return Enumerable.Empty<PatchOperation>();
        }
    }

    public abstract class PatchSettingItem : SettingItemBase
    {
        public PatchElement Patch;

        protected IEnumerable<PatchOperation> Patches => Patch?.Patches ?? Enumerable.Empty<PatchOperation>();
    }

    public class PatchElement
    {
        public XmlNode rootNode;
        public List<PatchOperation> Patches { get; private set; }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            rootNode = xmlRoot;
            Patches = LoadDataFromXml(xmlRoot);
        }

        public static List<PatchOperation> LoadDataFromXml(XmlNode xmlRoot)
        {
            return xmlRoot.ChildNodes.Cast<XmlNode>()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(n => n as XmlElement)
                .Where(e => e != null)
                .Where(e => e.Name == "Operation")
                .Select(e => DirectXmlToObject.ObjectFromXml<PatchOperation>(e, false))
                .ToList();
        }
    }

    public abstract class ContainerRowBase : ISettingRow
    {
        protected List<ISettingRow> rows = new List<ISettingRow>();

        public bool RequireReboot => rows.Any(r => r.RequireReboot);

        public void Apply()
        {
            rows.ForEach(r => r.Apply());
        }

        public abstract void Draw(Listing_Standard list);

        public void ExposeData()
        {
            rows.ForEach(r => r.ExposeData());
        }

        public IEnumerable<PatchOperation> GetValidPatches()
        {
            return rows.SelectMany(r => r.GetValidPatches());
        }

        public abstract bool Initialize();
    }

    public class RowsElement
    {
        public List<ISettingRow> rows;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            rows = LoadDataFromXml(xmlRoot);
        }

        public static List<ISettingRow> LoadDataFromXml(XmlNode xmlRoot)
        {
            return xmlRoot.ChildNodes.Cast<XmlNode>()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(n => n as XmlElement)
                .Where(e => e != null)
                .Where(e => e.Name == "Row")
                .Select(e => DirectXmlToObject.ObjectFromXml<ISettingRow>(e, false))
                .ToList();
        }
    }

    public class ContainerRow : ContainerRowBase
    {
        public Color backgroundColor = Color.clear;

        private float lastHeight = 100000;
        public RowsElement Rows = new RowsElement();

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(lastHeight);
            if (backgroundColor != Color.clear) Widgets.DrawRectFast(rect, backgroundColor);
            var child = new Listing_Standard();
            child.Begin(rect);
            rows.ForEach(r => r.Draw(child));
            child.End();
            lastHeight = child.CurHeight;
            list.Gap(list.verticalSpacing);
        }

        public override bool Initialize()
        {
            if (Rows == null || Rows.rows == null) return false;
            rows = Rows.rows.Where(r => r.Initialize()).ToList();
            return rows.Count > 0;
        }
    }

    public class SplitRow : ContainerRowBase
    {
        private float lastHeight = 300;

        public Color leftBackgroundColor = Color.clear;

        public ISettingRow LeftRow;
        public float rate = 0.5f;

        public Color rightBackgroundColor = Color.clear;

        public ISettingRow RightRow;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(lastHeight);

            var lr = new[]
            {
                new
                {
                    Row = LeftRow, List = new Listing_Standard(), Rect = rect.LeftPart(rate),
                    BGColor = leftBackgroundColor
                },
                new
                {
                    Row = RightRow, List = new Listing_Standard(), Rect = rect.RightPart(1f - rate),
                    BGColor = rightBackgroundColor
                }
            }.ToList();

            lr.ForEach(s =>
            {
                if (s.BGColor != Color.clear) Widgets.DrawRectFast(s.Rect, s.BGColor);
                s.List.Begin(s.Rect);
                s.Row.Draw(s.List);
                s.List.End();
            });
            lastHeight = lr.Select(s => s.List.CurHeight).Max();

            list.Gap(list.verticalSpacing);
        }

        public override bool Initialize()
        {
            rows = new[] {LeftRow, RightRow}.Where(r => r.Initialize()).ToList();
            return rows.Count > 0;
        }
    }

    public class TextRow : SettingRow
    {
        public TextAnchor anchor = TextAnchor.MiddleLeft;
        public Color backgroundColor = Color.clear;
        public GameFont font = GameFont.Small;
        public float height;
        public bool noTranslate = false;
        public string text = "";

        public override void Draw(Listing_Standard list)
        {
            var tmp = Text.Font;
            var tmpAnc = Text.Anchor;
            try
            {
                Text.Font = font;
                Text.Anchor = anchor;
                var h = height;
                var t = text.Translate();
                if (h == 0) h = Text.CalcHeight(t, list.ColumnWidth);

                var rect = list.GetRect(h);
                if (backgroundColor != Color.clear) Widgets.DrawRectFast(rect, backgroundColor);
                var label = text.Translate();
                if (noTranslate) label = text;
                Widgets.Label(rect, label);
                list.Gap(list.verticalSpacing);
            }
            finally
            {
                Text.Font = tmp;
                Text.Anchor = tmpAnc;
            }
        }
    }

    public class ImageRow : SettingRow
    {
        public Color backgroundColor = Color.clear;
        public float height;
        public string texPath;

        public override void Draw(Listing_Standard list)
        {
            var tex = ContentFinder<Texture2D>.Get(texPath);
            var h = height;
            if (h == 0) h = tex.height;
            var rect = list.GetRect(h);
            if (backgroundColor != Color.clear) Widgets.DrawRectFast(rect, backgroundColor);

            Widgets.DrawTextureFitted(rect, tex, 1);
            list.Gap(list.verticalSpacing);
        }
    }

    public class GapLineRow : SettingRow
    {
        public Color color = Color.clear;
        public float height = 12f;

        public override void Draw(Listing_Standard list)
        {
            var tmp = GUI.color;
            try
            {
                if (color != Color.clear) GUI.color = color;
                if (height != 0f) list.GapLine(height);
            }
            finally
            {
                GUI.color = tmp;
            }
        }
    }

    public class GapRow : SettingRow
    {
        public float height = 12f;

        public override void Draw(Listing_Standard list)
        {
            if (height != 0f) list.Gap(height);
        }
    }


    public class PatchItem : PatchSettingItem
    {
        private bool checkOn;
        private bool currentCheckOn;

        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return checkOn ? Patches : Enumerable.Empty<PatchOperation>();
        }

        public override void Apply()
        {
            if (currentCheckOn != checkOn)
            {
                RequireReboot = true;
                checkOn = currentCheckOn;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            list.CheckboxLabeled(label.Translate(), ref currentCheckOn, description.Translate());
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref checkOn, key);
            if (Scribe.mode != LoadSaveMode.Saving) currentCheckOn = checkOn;
        }
    }

    public abstract class PatchValueItem : PatchSettingItem
    {
        public bool checkOn;
        protected bool currentCheckOn;

        protected List<PatchOperation> replaced;

        protected abstract string ReplaceText { get; }

        protected IEnumerable<PatchOperation> ReplacedPatch
        {
            get
            {
                if (replaced == null)
                {
                    var xmlText = Patch.rootNode.OuterXml.Replace("${value}", SecurityElement.Escape(ReplaceText));
                    var doc = new XmlDocument();
                    doc.LoadXml(xmlText);
                    var p = PatchElement.LoadDataFromXml(doc.FirstChild);
                    replaced = PatchElement.LoadDataFromXml(doc.FirstChild);
                }

                return replaced;
            }
        }

        public override void Apply()
        {
            if (checkOn != currentCheckOn)
            {
                RequireReboot = true;
                checkOn = currentCheckOn;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref checkOn, key + "__check");
            if (Scribe.mode != LoadSaveMode.Saving) currentCheckOn = checkOn;
        }

        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return checkOn ? ReplacedPatch : Enumerable.Empty<PatchOperation>();
        }
    }

    public abstract class PatchValueItem<T> : PatchValueItem
    {
        protected T currentValue;
        public T value;

        protected override string ReplaceText => value.ToString();

        public override void Apply()
        {
            base.Apply();
            if (!Equals(value, currentValue))
            {
                RequireReboot = true;
                value = currentValue;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref value, key);
            if (Scribe.mode != LoadSaveMode.Saving) currentValue = value;
        }

        public override bool Initialize()
        {
            currentValue = value;
            return base.Initialize();
        }
    }

    public class PatchTextValueItem : PatchValueItem<string>
    {
        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);
            currentValue = Widgets.TextField(rect.RightHalf(), currentValue);
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchFloatValueItem : PatchValueItem<float>
    {
        public float maxValue = 100000f;
        public float minValue;

        public float roundTo = -1;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight * 2f);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);

            var rectSlider = rect.RightHalf();
            rectSlider.xMin += 20;
            rectSlider.xMax -= 20;
            currentValue = Widgets.HorizontalSlider(rectSlider, currentValue, minValue, maxValue, true,
                currentValue.ToString(), minValue.ToString(), maxValue.ToString(), roundTo);
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchIntValueItem : PatchValueItem<int>
    {
        public int maxValue = 100000;
        public int minValue;

        public int roundTo = 1;

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight * 2f);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);

            var rectSlider = rect.RightHalf();
            rectSlider.xMin += 20;
            rectSlider.xMax -= 20;
            currentValue = (int) Widgets.HorizontalSlider(rectSlider, currentValue, minValue, maxValue, true,
                currentValue.ToString(), minValue.ToString(), maxValue.ToString(), roundTo);
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchBoolValueItem : PatchValueItem<bool>
    {
        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);

            Widgets.Checkbox(rect.RightHalf().position, ref currentValue);
            list.Gap(list.verticalSpacing);
        }
    }

    public class PatchEnumValueItem : PatchValueItem<int>
    {
        public Type enumType;

        public List<object> EnumValues => enumType.GetEnumValues().Cast<object>().ToList();

        protected override string ReplaceText => EnumValues[value].ToString();

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);
            if (Widgets.ButtonText(rect.RightHalf(),
                "PRF.Settings.Select".Translate() + " (" + EnumValues[currentValue] + ")"))
                Find.WindowStack.Add(new FloatMenu(enumType.GetEnumValues().Cast<object>()
                    .Select((o, idx) => new FloatMenuOption(o.ToString(), () => currentValue = idx)).ToList()));

            list.Gap(list.verticalSpacing);
        }

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;
            if (enumType == null || enumType.GetEnumValues().Cast<object>().Count() == 0)
            {
                Log.Error("invalid enumType on Settings.xml");
                return false;
            }

            return true;
        }
    }

    public class PatchSelectValueItem : PatchValueItem<int>
    {
        public List<string> options;

        protected override string ReplaceText => options[value];

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(label.Translate(), description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref currentCheckOn);
            if (Widgets.ButtonText(rect.RightHalf(),
                "PRF.Settings.Select".Translate() + " (" + options[currentValue] + ")"))
                Find.WindowStack.Add(new FloatMenu(options
                    .Select((o, idx) => new FloatMenuOption(o.ToString(), () => currentValue = idx)).ToList()));

            list.Gap(list.verticalSpacing);
        }

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;
            if (options == null || options.Count == 0)
            {
                Log.Error("invalid selectionList on Settings.xml");
                return false;
            }

            return true;
        }
    }
}