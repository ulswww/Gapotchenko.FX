﻿using Gapotchenko.FX.Reflection.Pal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gapotchenko.FX.Reflection
{
    /// <summary>
    /// Provides services with a controlled lifespan for automatic assembly resolution and dynamic loading based on specified probing paths, binding redirects and common sense heuristics.
    /// </summary>
    sealed class AssemblyAutoLoaderDomain : IDisposable
    {
        readonly Dictionary<Assembly, AssemblyDescriptor> m_AssemblyDescriptors = new Dictionary<Assembly, AssemblyDescriptor>();

        /// <summary>
        /// Adds a specified assembly to the list of sources to consider during assembly resolution process for the current app domain.
        /// Once added, the loader automatically handles binding redirects according to a corresponding assembly configuration (<c>.config</c>) file.
        /// If configuration file is missing then binding redirects are automatically deducted according to the assembly compatibility heuristics.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns><c>true</c> if the assembly is added; <c>false</c> if the assembly is already added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> parameter is <c>null</c>.</exception>
        public bool AddAssembly(Assembly assembly) => AddAssembly(assembly, null);

        /// <summary>
        /// Adds a specified assembly to the list of sources to consider during assembly resolution process for the current app domain.
        /// Once added, the loader automatically handles binding redirects according to a corresponding assembly configuration (<c>.config</c>) file.
        /// If configuration file is missing then binding redirects are automatically deducted according to the assembly compatibility heuristics.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="additionalProbingPaths">The additional probing paths for dependencies of a specified assembly.</param>
        /// <returns><c>true</c> if the assembly with the specified set of additional probing paths is added; <c>false</c> if the assembly with the specified set of additional probing paths is already added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> parameter is <c>null</c>.</exception>
        public bool AddAssembly(Assembly assembly, params string[] additionalProbingPaths)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            lock (m_AssemblyDescriptors)
            {
                if (m_AssemblyDescriptors.TryGetValue(assembly, out var descriptor))
                {
                    return descriptor.AddProbingPaths(additionalProbingPaths);
                }
                else
                {
                    m_AssemblyDescriptors.Add(assembly, new AssemblyDescriptor(assembly, additionalProbingPaths));
                    return true;
                }
            }
        }

        /// <summary>
        /// Removes a specified assembly from the list of sources to consider during assembly resolution process for the current app domain.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns><c>true</c> if the assembly is removed; <c>false</c> if the assembly already removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> parameter is <c>null</c>.</exception>
        public bool RemoveAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            AssemblyDescriptor descriptor;
            lock (m_AssemblyDescriptors)
                if (!m_AssemblyDescriptors.TryGetValue(assembly, out descriptor))
                    return false;

            descriptor.Dispose();
            return true;
        }

        readonly Dictionary<string, ProbingPathAssemblyLoader> m_ProbingPathResolvers = new Dictionary<string, ProbingPathAssemblyLoader>(FileSystem.PathComparer);

        /// <summary>
        /// Adds a specified probing path for the current app domain.
        /// Once added, establishes the specified directory path as the location of assemblies to probe during assembly resolution process.
        /// </summary>
        /// <param name="path">The probing path.</param>
        public bool AddProbingPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = Path.GetFullPath(path);

            lock (m_ProbingPathResolvers)
            {
                if (m_ProbingPathResolvers.ContainsKey(path))
                    return false;

                m_ProbingPathResolvers.Add(path, new ProbingPathAssemblyLoader(path));
                return true;
            }
        }

        /// <summary>
        /// Removes a specified probing path for the current app domain.
        /// Once removed, ceases to treat the specified directory path as the location of assemblies to probe during assembly resolution process.
        /// </summary>
        /// <param name="path">The probing path.</param>
        public bool RemoveProbingPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            path = Path.GetFullPath(path);

            ProbingPathAssemblyLoader loader;
            lock (m_ProbingPathResolvers)
                if (!m_ProbingPathResolvers.TryGetValue(path, out loader))
                    return false;

            loader.Dispose();
            return true;
        }

        public void Dispose()
        {
            var disposables = new List<IDisposable>();

            lock (m_AssemblyDescriptors)
            {
                disposables.AddRange(m_AssemblyDescriptors.Values);
                m_AssemblyDescriptors.Clear();
            }

            lock (m_ProbingPathResolvers)
            {
                disposables.AddRange(m_ProbingPathResolvers.Values);
                m_ProbingPathResolvers.Clear();
            }

            foreach (var i in disposables)
                i.Dispose();
        }
    }
}