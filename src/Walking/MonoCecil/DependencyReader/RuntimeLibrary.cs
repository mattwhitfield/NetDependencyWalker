// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class RuntimeLibrary : Library
    {
        public RuntimeLibrary(string type, string name, string version, string hash, IReadOnlyList<RuntimeAssetGroup> runtimeAssemblyGroups, IReadOnlyList<RuntimeAssetGroup> nativeLibraryGroups, IEnumerable<ResourceAssembly> resourceAssemblies, IEnumerable<Dependency> dependencies, bool serviceable, string path, string hashPath, string runtimeStoreManifestName)
            : base(type, name, version, hash, dependencies, serviceable, path, hashPath, runtimeStoreManifestName)
        {
            RuntimeAssemblyGroups = runtimeAssemblyGroups ?? throw new ArgumentNullException(nameof(runtimeAssemblyGroups));
            ResourceAssemblies = resourceAssemblies?.ToArray() ?? throw new ArgumentNullException(nameof(resourceAssemblies));
            NativeLibraryGroups = nativeLibraryGroups ?? throw new ArgumentNullException(nameof(nativeLibraryGroups));
        }

        public IReadOnlyList<RuntimeAssetGroup> RuntimeAssemblyGroups { get; }

        public IReadOnlyList<RuntimeAssetGroup> NativeLibraryGroups { get; }

        public IReadOnlyList<ResourceAssembly> ResourceAssemblies { get; }
    }
}