using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using HarmonyLib;
using Verse;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Settings : DefModExtension
    {
        public List<SettingsListItem> settings;

        public T GetByName<T>(string name)
        {
            var op = settings?.Where(o => o.name == name).FirstOrDefault();

            if (op == null) return default;
            if (op.noClass)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T) (object) op.value.ToString();
                }

                Parser<T>.TryParse(op.value.ToString(), out var value);
                return value;
            }

            return (T) op.value;
        }

        public static class Parser<T>
        {
            private static readonly TryParseDelegate tryParse;

            static Parser()
            {
                var method = AccessTools.Method(typeof(T), "TryParse");
                if (method == null) return;
                var outValue = Expression.Parameter(typeof(T).MakeByRefType());
                var stringValue = Expression.Parameter(typeof(string));

                tryParse = Expression.Lambda<TryParseDelegate>(
                    Expression.Call(method, stringValue, outValue),
                    stringValue,
                    outValue
                ).Compile();
            }

            public static bool TryParse(string text, out T value)
            {
                if (tryParse == null)
                {
                    value = default;
                    return false;
                }

                return tryParse(text, out value);
            }

            private delegate bool TryParseDelegate(string input, out T value);
        }
    }

    public class SettingsListItem
    {
        public string name;

        public bool noClass = true;

        public object value;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            name = xmlRoot.Name;
            noClass = xmlRoot.Attributes["Class"] == null;
            if (noClass)
                value = xmlRoot.InnerText;
            else
                value = DirectXmlToObject.ObjectFromXml<object>(xmlRoot, false);
        }
    }
}