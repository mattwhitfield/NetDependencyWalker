namespace NetDependencyWalker.Walking.Native
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;

    public class NativeWalker : IWalker
    {
        private readonly Dictionary<string, AssemblyDetail> _assembliesByName = new Dictionary<string, AssemblyDetail>(StringComparer.OrdinalIgnoreCase);
        private AssemblyDetail _root;
        
        private readonly Queue<NativeAssemblyLoader> _loadQueue = new Queue<NativeAssemblyLoader>();
        private int _referencesProcessed;

        public AssemblySet WalkFrom(string rootFileName)
        {
            var loadContextWeakReference = Walk(rootFileName);

            for (var i = 0; loadContextWeakReference.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (loadContextWeakReference.IsAlive)
                {
                    Thread.Sleep(50);
                }
            }

            return new AssemblySet(_root, _assembliesByName, _referencesProcessed, string.Empty);
        }

        private WeakReference Walk(string rootFileName)
        {
            var loadContext = new NativeWalkerAssemblyLoadContext();
            var loadContextWeakReference = new WeakReference(loadContext);
            _loadQueue.Enqueue(new NativeAssemblyLoader(rootFileName, null, (context, filePath) => context.LoadFromAssemblyPath(filePath)));

            while (_loadQueue.Count > 0)
            {
                var current = _loadQueue.Dequeue();
                InspectAssembly(current, loadContext);
            }

            loadContext.Unload();
            return loadContextWeakReference;
        }

        private static string GetCodeBasePath(Assembly assembly)
        {
            var codeBase = assembly.CodeBase;
            if (string.IsNullOrWhiteSpace(codeBase))
            {
                return Path.GetDirectoryName(assembly.Location);
            }

            try
            {
                var uri = new UriBuilder(codeBase);
                if (!string.IsNullOrWhiteSpace(uri.Path))
                {
                    var path = Uri.UnescapeDataString(uri.Path);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        return Path.GetDirectoryName(path);
                    }
                }
            }
            catch (UriFormatException)
            {
            }
            catch (ArgumentException)
            {
            }
            catch (PathTooLongException)
            {
            }

            return null;
        }

        private void InspectAssembly(NativeAssemblyLoader loader, NativeWalkerAssemblyLoadContext context)
        {
            AssemblyDetail tn;
            try
            {
                Assembly assembly;
                try
                {
                    assembly = loader.Load(context, loader.Name);
                }
                catch
                {
                    assembly = null;
                }

                if (assembly == null)
                {
                    assembly = loader.Load(AssemblyLoadContext.Default, loader.Name);

                    if (assembly == null)
                    {
                        tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };
                        tn.Errors.Add("Could not locate assembly '" + loader.Name + "'.");

                        _assembliesByName[tn.IdentityName] = tn;
                        return;
                    }
                }


                var codeBasePath = GetCodeBasePath(assembly);
                context.AddPath(codeBasePath);
                var assemblyName = assembly.GetName();
                tn = new AssemblyDetail(assemblyName.Name, codeBasePath, assemblyName.Version);
                if (_assembliesByName.TryGetValue(tn.IdentityName, out var existingDetail))
                {
                    existingDetail.AddVersion(loader.Version);
                    return;
                }

                var referencedAssemblies = assembly.GetReferencedAssemblies();
                _referencesProcessed += referencedAssemblies.Length;
                foreach (var an in referencedAssemblies)
                {
                    if (string.IsNullOrWhiteSpace(an.Name))
                    {
                        continue;
                    }

                    tn.ReferencedAssemblies.Add(new ReferencedAssembly(an.Name, an.Version));
                    _loadQueue.Enqueue(new NativeAssemblyLoader(an.FullName, an.Version, (ctx, name) => ctx.LoadFromAssemblyName(new AssemblyName(name))));
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
            catch (FileNotFoundException ex)
            {
                tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };
                tn.Errors.Add("FileNotFoundException: " + ex.Message);
            }
            catch (Exception ex)
            {
                tn = new AssemblyDetail(loader.Name, null, null) { HasLoadingErrors = true };
                tn.Errors.Add(ex.ToString());
            }

            _assembliesByName[tn.IdentityName] = tn;
            _root ??= tn;
        }
    }
}
