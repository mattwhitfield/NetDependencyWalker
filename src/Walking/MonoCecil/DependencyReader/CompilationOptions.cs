// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    internal class CompilationOptions
    {
        public IList<string> Defines { get; set;}

        public string LanguageVersion { get;set; }

        public string Platform { get; set;}

        public bool? AllowUnsafe { get; set;}

        public bool? WarningsAsErrors { get;set; }

        public bool? Optimize { get;set; }

        public string KeyFile { get;set; }

        public bool? DelaySign { get;set; }

        public bool? PublicSign { get;set; }

        public string DebugType { get;set; }

        public bool? EmitEntryPoint { get;set; }

        [JsonProperty("xmlDoc")]
        public bool? GenerateXmlDocumentation { get; set; }

        public static CompilationOptions Default { get; } = new CompilationOptions { Defines = new string[0] };
    }
}