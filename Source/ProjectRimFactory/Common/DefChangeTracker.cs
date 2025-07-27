using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common
{
    /// <summary>
    /// Class created by lilwhitemouse and licenced under GPL-3.0 / LGPL
    /// Original Class: https://github.com/lilwhitemouse/RimWorld-LWM.DeepStorage/blob/abad0836d3c9764e52ecff1d6efe9f680bd7eb1a/DeepStorage/DefChangeTracker.cs
    /// 
    /// Changes:
    /// Removed DS Specific Code
    /// Removed Unused Code
    /// Style Updates / ReSharper / new C# versions
    /// </summary>
    //   Default values for defs, so that when saving mod settings, we know what the defaults are.
    public class DefChangeTracker
    {
        private static readonly Dictionary<string, object> DefaultDefValues = new();
        
        public void AddDefaultValue(string defName, string keylet, object defaultValue)
        {
            DefaultDefValues[defName + "_" + keylet] = defaultValue;
        }

        public T GetDefaultValue<T>(string defName, string keylet) where T : class
        {
            return GetDefaultValue<T>(defName, keylet, null);
        }

        public T GetDefaultValue<T>(string defName, string keylet, T defaultValue)
        {
            if (DefaultDefValues.ContainsKey(defName + "_" + keylet))
                return (T)DefaultDefValues[defName + "_" + keylet];
            return defaultValue;
        }
        

        public IEnumerable<T> GetAllWithKeylet<T>(string keylet)
        {
            foreach (var entry in DefaultDefValues)
            {
                var t = entry.Key.Split('_');
                if (t[t.Length - 1] == keylet)
                {
                    yield return (T)entry.Value;
                }
            }
        }

    }

}
