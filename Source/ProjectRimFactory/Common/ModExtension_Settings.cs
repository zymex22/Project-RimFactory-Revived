using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Xml;
using System.ComponentModel;
using HarmonyLib;
using System.Linq.Expressions;

namespace ProjectRimFactory.Common
{
    public class ModExtension_Settings : DefModExtension
    {
        public List<SettingsListItem> settings;

        public T GetByName<T>(string name)
        {
            var op = this.settings?.Where(o => o.name == name).FirstOrDefault();

            if(op == null)
            {
                return default(T);
            }
            if (op.noClass)
            {
                if(typeof(T) == typeof(string))
                {
                    return (T)(object)op.value.ToString();
                }
                else
                {
                    Parser<T>.TryParse(op.value.ToString(), out T value);
                    return value;
                }
            }
            else
            {
                return (T)op.value;
            }
        }

        public static class Parser<T>
        {
            private delegate bool TryParseDelegate(string input, out T value);

            private static TryParseDelegate tryParse;

            static Parser()
            {
                var method = HarmonyLib.AccessTools.Method(typeof(T), "TryParse");
                if (method == null)
                {
                    return;
                }
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
                    value = default(T);
                    return false;
                }
                return tryParse(text, out value);
            }
        }
    }

    public class SettingsListItem
    {
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            this.name = xmlRoot.Name;
            this.noClass = xmlRoot.Attributes["Class"] == null;
            if (this.noClass)
            {
                this.value = xmlRoot.InnerText;
            }
            else
            {
                this.value = DirectXmlToObject.ObjectFromXml<object>(xmlRoot, false);
            }
        }

        public string name;

        public object value;

        public bool noClass = true;
    }
}
