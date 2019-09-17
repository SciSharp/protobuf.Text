using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Protobuf.Reflection;

namespace Protobuf.Text
{
        // Effectively a cache of mapping from enum values to the original name as specified in the proto file,
        // fetched by reflection.
        // The need for this is unfortunate, as is its unbounded size, but realistically it shouldn't cause issues.
        static class OriginalEnumValueHelper
        {
            // TODO: In the future we might want to use ConcurrentDictionary, at the point where all
            // the platforms we target have it.
            private static readonly Dictionary<System.Type, Dictionary<object, string>> dictionaries
                = new Dictionary<System.Type, Dictionary<object, string>>();

            internal static string GetOriginalName(object value)
            {
                var enumType = value.GetType();
                Dictionary<object, string> nameMapping;
                lock (dictionaries)
                {
                    if (!dictionaries.TryGetValue(enumType, out nameMapping))
                    {
                        nameMapping = GetNameMapping(enumType);
                        dictionaries[enumType] = nameMapping;
                    }
                }

                string originalName;
                // If this returns false, originalName will be null, which is what we want.
                nameMapping.TryGetValue(value, out originalName);
                return originalName;
            }

            private static Dictionary<object, string> GetNameMapping(System.Type enumType) =>
                enumType.GetTypeInfo().DeclaredFields
                    .Where(f => f.IsStatic)
                    .Where(f => f.GetCustomAttributes<OriginalNameAttribute>()
                                 .FirstOrDefault()?.PreferredAlias ?? true)
                    .ToDictionary(f => f.GetValue(null),
                                  f => f.GetCustomAttributes<OriginalNameAttribute>()
                                        .FirstOrDefault()
                                        // If the attribute hasn't been applied, fall back to the name of the field.
                                        ?.Name ?? f.Name);
        }
}