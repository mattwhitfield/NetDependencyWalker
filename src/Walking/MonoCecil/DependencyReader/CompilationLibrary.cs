// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class CompilationLibrary : Library
    {
        public CompilationLibrary(string type, string name, string version, string hash, IEnumerable<string> assemblies, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath)
            : base(type, name, version, hash, dependencies, serviceable, path, hashPath, null)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }
            Assemblies = assemblies.ToArray();
        }

        public IReadOnlyList<string> Assemblies { get; }
    }
}