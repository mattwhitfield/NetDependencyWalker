// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace NetDependencyWalker.Walking.MonoCecil.DependencyReader
{
    using System;

    internal readonly struct Dependency
    {
        public Dependency(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException(nameof(version));
            }
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }

        public bool Equals(Dependency other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Dependency dependency && Equals(dependency);
        }

        public override int GetHashCode()
        {
            return (Name.GetHashCode() * 27) ^ Version.GetHashCode();
        }
    }
}