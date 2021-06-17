// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class DependencyContext
    {
        public DependencyContext(TargetInfo target, CompilationOptions compilationOptions, IEnumerable<CompilationLibrary> compileLibraries, IEnumerable<RuntimeLibrary> runtimeLibraries, IEnumerable<RuntimeFallbacks> runtimeGraph)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            CompilationOptions = compilationOptions ?? throw new ArgumentNullException(nameof(compilationOptions));
            CompileLibraries = compileLibraries?.ToArray() ?? throw new ArgumentNullException(nameof(compileLibraries));
            RuntimeLibraries = runtimeLibraries?.ToArray() ?? throw new ArgumentNullException(nameof(runtimeLibraries));
            RuntimeGraph = runtimeGraph?.ToArray() ?? throw new ArgumentNullException(nameof(runtimeGraph));
        }

        public TargetInfo Target { get; }

        public CompilationOptions CompilationOptions { get; }

        public IReadOnlyList<CompilationLibrary> CompileLibraries { get; }

        public IReadOnlyList<RuntimeLibrary> RuntimeLibraries { get; }

        public IReadOnlyList<RuntimeFallbacks> RuntimeGraph { get; }
    }
}