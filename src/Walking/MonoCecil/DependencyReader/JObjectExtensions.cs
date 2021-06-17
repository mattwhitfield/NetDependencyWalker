// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public static class JObjectExtensions
    {
        public static JObject ValueAsJsonObject(this JObject source, string key)
        {
            return source.GetValue(key) as JObject;
        }

        public static string ValueAsString(this JObject source, string key)
        {
            return (source.GetValue(key) as JValue)?.Value?.ToString();
        }

        public static bool? ValueAsNullableBoolean(this JObject source, string key)
        {
            return (source.GetValue(key) as JValue)?.ToObject<bool>();
        }

        public static string[] ValueAsStringArray(this JObject source, string key)
        {
            return source.GetValue(key)?.Values<string>().ToArray();
        }
    }
}
