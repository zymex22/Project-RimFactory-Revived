using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ProjectRimFactory_ModSettings : ModSettings
    {
        public static void LoadXml(ModContentPack content)
        {
            rows = ParseSettingRows(content).ToList();
        }

        private static List<ISettingRow> rows;

        public static IEnumerable<ISettingItem> SettingItems => rows.Select(r => r as ISettingItem).Where(i => i != null);

        public static IEnumerable<IPatchItem> PatchItems => rows.Select(r => r as IPatchItem).Where(i => i != null);

        private static IEnumerable<ISettingRow> ParseSettingRows(ModContentPack content)
        {
            var xmlDoc = DirectXmlLoader.XmlAssetsInModFolder(content, "Settings").Where(x => x.name == "Settings.xml").ToList().FirstOrDefault();
            if (xmlDoc == null)
            {
                Log.Error("Settings/Settings.xml not found.");
                return Enumerable.Empty<ISettingRow>();
            }
            var rootElem = xmlDoc.xmlDoc.DocumentElement;
            if (rootElem.Name != "SettingRows")
            {
                Log.Error("SettingRows not found. name=" + rootElem.Name);
                return Enumerable.Empty<ISettingRow>();
            }
            return rootElem.ChildNodes.Cast<XmlNode>()
                .Where(n => n.NodeType == XmlNodeType.Element)
                .Select(n => n as XmlElement)
                .Where(e => e != null)
                .Select(e => DirectXmlToObject.ObjectFromXml<ISettingRow>(e, false))
                .Where(r => (r as ISettingItem)?.IsValidConfig() ?? true);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            SettingItems.ToList().ForEach(i => i.ExposeData());
        }

        public void DoWindowContents(Rect inRect)
        {
            Rect outRect = new Rect(inRect);
            outRect.yMin += 20f;
            outRect.yMax -= 20f;
            outRect.xMin += 20f;
            outRect.xMax -= 20f;

            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, this.lastHeight);

            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            var list = new Listing_Standard();
            list.Begin(viewRect);
            rows.ForEach(r => r.Draw(list));
            list.End();
            Widgets.EndScrollView();

            this.lastHeight = list.CurHeight + 16f;
        }

        private Vector2 scrollPosition;
        private float lastHeight = 1000f;

        public void Apply()
        {
            SettingItems.ToList().ForEach(i => i.Apply());
        }

        public bool RequireReboot => SettingItems.Any(i => i.RequireReboot);

        public virtual IEnumerable<PatchOperation> Patches => PatchItems.SelectMany(i => i.GetValidPatches());
    }

    public interface ISettingRow
    {
        void Draw(Listing_Standard list);
    }

    public interface ISettingItem : ISettingRow
    {
        void ExposeData();
        void Apply();
        bool RequireReboot { get; }
        bool IsValidConfig();
    }

    public interface IPatchItem : ISettingItem
    {
        IEnumerable<PatchOperation> GetValidPatches();
    }

    public class TextRow : ISettingRow
    {
        public GameFont font = GameFont.Small;
        public TextAnchor anchor = TextAnchor.MiddleRight;
        public string text = "";
        public void Draw(Listing_Standard list)
        {
            var tmp = Text.Font;
            var tmpAnc = Text.Anchor;
            try
            {
                Text.Font = this.font;
                Text.Anchor = this.anchor;
                list.Label(this.text.Translate());
            }
            finally
            {
                Text.Font = tmp;
                Text.Anchor = tmpAnc;
            }
        }
    }

    public class GapLineRow : ISettingRow
    {
        public float height = 12f;
        public Color color = Color.clear;
        public void Draw(Listing_Standard list)
        {
            Color tmp = GUI.color;
            try
            {
                if(this.color != Color.clear)
                {
                    GUI.color = this.color;
                }
                if (height != 0f)
                {
                    list.GapLine(height);
                }
            }
            finally
            {
                GUI.color = tmp;
            }
        }
    }

    public class GapRow : ISettingRow
    {
        public float height = 12f;
        public void Draw(Listing_Standard list)
        {
            if (height != 0f)
            {
                list.Gap(this.height);
            }
        }
    }

    public abstract class SettingItem : ISettingItem
    {
        public string key;
        public string label;
        public string description;
        public virtual bool RequireReboot { get; protected set; } = false;

        public abstract void Draw(Listing_Standard list);

        public abstract void ExposeData();

        public abstract void Apply();

        public virtual bool IsValidConfig()
        {
            return true;
        }
    }

    public abstract class PatchSettingItem : SettingItem, IPatchItem
    {
        public abstract IEnumerable<PatchOperation> GetValidPatches();

        public PatchElement Patch;

        protected IEnumerable<PatchOperation> Patches => this.Patch?.Patches ?? Enumerable.Empty<PatchOperation>();
    }

    public class PatchElement
    {
        public List<PatchOperation> Patches { get; private set; }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            this.rootNode = xmlRoot;
            this.Patches = LoadDataFromXml(xmlRoot);
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

        public XmlNode rootNode;
    }

    public class PatchItem : PatchSettingItem
    {
        private bool checkOn = false;
        private bool currentCheckOn;

        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return this.checkOn ? this.Patches : Enumerable.Empty<PatchOperation>();
        }

        public override void Apply()
        {
            if(this.currentCheckOn != this.checkOn)
            {
                this.RequireReboot = true;
                this.checkOn = this.currentCheckOn;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            list.CheckboxLabeled(this.label.Translate(), ref this.currentCheckOn, this.description.Translate());
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.checkOn, this.key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentCheckOn = this.checkOn;
            }
        }
    }

    public abstract class PatchValueItem : PatchSettingItem
    {
        public bool checkOn = false;

        protected bool currentCheckOn;

        protected abstract string ReplaceText { get; }

        protected List<PatchOperation> replaced;

        protected IEnumerable<PatchOperation> ReplacedPatch
        {
            get
            {
                if (this.replaced == null)
                {
                    var xmlText = this.Patch.rootNode.OuterXml.Replace("${value}", SecurityElement.Escape(this.ReplaceText));
                    var doc = new XmlDocument();
                    doc.LoadXml(xmlText);
                    var p = PatchElement.LoadDataFromXml(doc.FirstChild);
                    this.replaced = PatchElement.LoadDataFromXml(doc.FirstChild);
                }
                return this.replaced;
            }
        }

        public override void Apply()
        {
            if (this.checkOn != this.currentCheckOn)
            {
                this.RequireReboot = true;
                this.checkOn = this.currentCheckOn;
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.checkOn, this.key + "__check");
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentCheckOn = this.checkOn;
            }
        }

        public override IEnumerable<PatchOperation> GetValidPatches()
        {
            return this.checkOn ? this.ReplacedPatch : Enumerable.Empty<PatchOperation>();
        }
    }

    public class PatchTextValueItem : PatchValueItem
    {
        public string value = "";

        private string currentValue;

        public override void Apply()
        {
            base.Apply();
            if(this.value != this.currentValue)
            {
                this.RequireReboot = true;
                this.value = this.currentValue;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(this.label.Translate(), this.description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref this.currentCheckOn);
            this.currentValue = Widgets.TextField(rect.RightHalf(), this.currentValue);
            list.Gap(list.verticalSpacing);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.value, this.key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentValue = this.value;
            }
        }

        protected override string ReplaceText => this.value;
    }

    public class PatchFloatValueItem : PatchValueItem
    {
        public float value = 0f;

        public float minValue = 0f;

        public float maxValue = 100000f;

        public float roundTo = -1;

        private float currentValue;

        public override void Apply()
        {
            base.Apply();
            if (this.value != this.currentValue)
            {
                this.RequireReboot = true;
                this.value = this.currentValue;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight * 2f);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(this.label.Translate(), this.description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref this.currentCheckOn);

            var rectSlider = rect.RightHalf();
            rectSlider.xMin += 20;
            rectSlider.xMax -= 20;
            this.currentValue = Widgets.HorizontalSlider(rectSlider, this.currentValue, this.minValue, this.maxValue, true, this.currentValue.ToString(), this.minValue.ToString(), this.maxValue.ToString(), this.roundTo);
            list.Gap(list.verticalSpacing);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.value, this.key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentValue = this.value;
            }
        }

        protected override string ReplaceText => this.value.ToString();
    }

    public class PatchIntValueItem : PatchValueItem
    {
        public int value = 0;

        public int minValue = 0;

        public int maxValue = 100000;

        public int roundTo = 1;

        private int currentValue;

        public override void Apply()
        {
            base.Apply();
            if (this.value != this.currentValue)
            {
                this.RequireReboot = true;
                this.value = this.currentValue;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight * 2f);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(this.label.Translate(), this.description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref this.currentCheckOn);

            var rectSlider = rect.RightHalf();
            rectSlider.xMin += 20;
            rectSlider.xMax -= 20;
            this.currentValue = (int)Widgets.HorizontalSlider(rectSlider, this.currentValue, this.minValue, this.maxValue, true, this.currentValue.ToString(), this.minValue.ToString(), this.maxValue.ToString(), this.roundTo);
            list.Gap(list.verticalSpacing);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.value, this.key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentValue = this.value;
            }
        }

        protected override string ReplaceText => this.value.ToString();
    }

    public class PatchEnumValueItem : PatchValueItem
    {
        public int value = 0;

        private int currentValue;

        public Type enumType;

        public override void Apply()
        {
            base.Apply();
            if (this.value != this.currentValue)
            {
                this.RequireReboot = true;
                this.value = this.currentValue;
            }
        }

        public object GetCurrentEnumValue()
        {
            return this.enumType.GetEnumValues().Cast<object>().ToList()[this.currentValue];
        }

        public object GetEnumValue()
        {
            return this.enumType.GetEnumValues().Cast<object>().ToList()[this.value];
        }

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(this.label.Translate(), this.description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref this.currentCheckOn);
            if (Widgets.ButtonText(rect.RightHalf(), "PRF.Settings.Select".Translate() + " (" + GetCurrentEnumValue() + ")"))
            {
                Find.WindowStack.Add(new FloatMenu(this.enumType.GetEnumValues().Cast<object>().Select((o, idx) => new FloatMenuOption(o.ToString(), () => this.currentValue = idx)).ToList()));
            }

            list.Gap(list.verticalSpacing);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.value, this.key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentValue = this.value;
            }
        }

        protected override string ReplaceText => this.GetEnumValue().ToString();

        public override bool IsValidConfig()
        {
            if(enumType == null || this.enumType.GetEnumValues().Cast<object>().Count() ==  0)
            {
                Log.Error("invalid enumType on Settings.xml");
                return false;
            }
            return true;
        }
    }

    public class PatchSelectValueItem : PatchValueItem
    {
        public int value = 0;

        private int currentValue;

        public List<string> options;

        public override void Apply()
        {
            base.Apply();
            if (this.value != this.currentValue)
            {
                this.RequireReboot = true;
                this.value = this.currentValue;
            }
        }

        public override void Draw(Listing_Standard list)
        {
            var rect = list.GetRect(Text.LineHeight);
            var left = rect.LeftHalf();
            Widgets.Label(left.LeftHalf(), new GUIContent(this.label.Translate(), this.description.Translate()));
            Widgets.Checkbox(left.RightHalf().position, ref this.currentCheckOn);
            if (Widgets.ButtonText(rect.RightHalf(), "PRF.Settings.Select".Translate() + " (" + this.options[this.currentValue] + ")"))
            {
                Find.WindowStack.Add(new FloatMenu(this.options.Select((o, idx) => new FloatMenuOption(o.ToString(), () => this.currentValue = idx)).ToList()));
            }

            list.Gap(list.verticalSpacing);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.value, this.key);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                this.currentValue = this.value;
            }
        }

        protected override string ReplaceText => this.options[this.value].ToString();

        public override bool IsValidConfig()
        {
            if (this.options == null || this.options.Count == 0)
            {
                Log.Error("invalid selectionList on Settings.xml");
                return false;
            }
            return true;
        }
    }
}
