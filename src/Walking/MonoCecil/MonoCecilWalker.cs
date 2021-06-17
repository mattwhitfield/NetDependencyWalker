namespace NetDependencyWalker.Walking.MonoCecil
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NetDependencyWalker.Walking.Native;

    public class MonoCecilWalker : IWalker
    {
        private readonly Dictionary<string, AssemblyDetail> _assembliesByName = new Dictionary<string, AssemblyDetail>(StringComparer.OrdinalIgnoreCase);
        private AssemblyDetail _root;
        
        private readonly Queue<MonoCecilAssemblyLoader> _loadQueue = new Queue<MonoCecilAssemblyLoader>();
        private int _referencesProcessed;

        public AssemblySet WalkFrom(string rootFileName)
        {
            var rootAssembly = new Ecma335File(rootFileName).Read();

            _resolver = new UniversalAssemblyResolver(rootFileName, rootAssembly.TargetFrameworkId);


            _loadQueue.Enqueue(new MonoCecilAssemblyLoader(rootFileName, null, () => rootAssembly));

            while (_loadQueue.Count > 0)
            {
                var current = _loadQueue.Dequeue();
                InspectAssembly(current);
            }

            return new AssemblySet(_root, _assembliesByName, _referencesProcessed, rootAssembly.TargetFrameworkId);
        }

        private UniversalAssemblyResolver _resolver;

        private void InspectAssembly(MonoCecilAssemblyLoader loader)
        {
            AssemblyDetail tn;
            try
            {
                var assembly = loader.Load();
                if (assembly == null)
                {
                    tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };
                    tn.Errors.Add("Could not locate assembly '" + loader.Name + "'.");

                    _assembliesByName[tn.IdentityName] = tn;
                    return;
                }

                _resolver.AddSearchDirectory(Path.GetDirectoryName(assembly.FileName));
                var assemblyName = assembly.Name;
                tn = new AssemblyDetail(assemblyName.Name, assembly.FileName, assemblyName.Version);
                if (_assembliesByName.TryGetValue(tn.IdentityName, out var existingDetail))
                {
                    existingDetail.AddVersion(loader.Version);
                    return;
                }

                _referencesProcessed += assembly.ReferencedAssemblies.Count;
                foreach (var an in assembly.ReferencedAssemblies)
                {
                    if (string.IsNullOrWhiteSpace(an.Name))
                    {
                        continue;
                    }

                    tn.ReferencedAssemblies.Add(new ReferencedAssembly(an.Name, an.Version));
                    _loadQueue.Enqueue(new MonoCecilAssemblyLoader(an.FullName, an.Version, () => FindAssembly(an).Read()));
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };

                if (ex.LoaderExceptions != null)
                {
                    foreach (var eee in ex.LoaderExceptions)
                    {
                        if (eee != null)
                        {
                            tn.Errors.Add(eee.ToString());
                        }
                    }
                }
            }
            catch (FileLoadException ex) when (ex.Message == loader.Name)
            {
                tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };
                tn.Errors.Add("Could not locate assembly '" + loader.Name + "'.");
            }
            catch (Exception ex)
            {
                tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };
                tn.Errors.Add(ex.ToString());
            }

            _assembliesByName[tn.IdentityName] = tn;
            _root ??= tn;
        }

        private Ecma335File FindAssembly(AssemblyName name)
        {
            var findAssemblyFile = _resolver.FindAssemblyFile(name);
            if (findAssemblyFile != null)
            {
                return new Ecma335File(findAssemblyFile);
            }

            throw new FileLoadException(name.Name);
        }
    }
}