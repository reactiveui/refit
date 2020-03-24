using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Task = Microsoft.Build.Utilities.Task;
#if NETCOREAPP2_1
using Microsoft.Build.Framework;
using System.Runtime.Loader;
#endif

namespace Refit.Generator.Tasks
{
    public abstract class ContextAwareTask : Task
    {
        protected virtual string ManagedDllDirectory => Path.GetDirectoryName(new Uri(GetType().GetTypeInfo().Assembly.CodeBase).LocalPath);

        //protected virtual string UnmanagedDllDirectory => null;

        public override bool Execute()
        {
#if NETCOREAPP2_1
            var taskAssemblyPath = new Uri(GetType().GetTypeInfo().Assembly.CodeBase).LocalPath;
            var ctxt = new CustomAssemblyLoader(this);
            var inContextAssembly = ctxt.LoadFromAssemblyPath(taskAssemblyPath);
            var innerTaskType = inContextAssembly.GetType(GetType().FullName);
            var innerTask = Activator.CreateInstance(innerTaskType);

            var outerProperties = GetType().GetRuntimeProperties().ToDictionary(i => i.Name);
            var innerProperties = innerTaskType.GetRuntimeProperties().ToDictionary(i => i.Name);
            var propertiesDiscovery = from outerProperty in outerProperties.Values
                                      where outerProperty.SetMethod != null && outerProperty.GetMethod != null
                                      let innerProperty = innerProperties[outerProperty.Name]
                                      select new { outerProperty, innerProperty };
            var propertiesMap = propertiesDiscovery.ToArray();
            var outputPropertiesMap = propertiesMap.Where(pair => pair.outerProperty.GetCustomAttribute<OutputAttribute>() != null).ToArray();

            foreach (var propertyPair in propertiesMap)
            {
                var outerPropertyValue = propertyPair.outerProperty.GetValue(this);
                propertyPair.innerProperty.SetValue(innerTask, outerPropertyValue);
            }

            var executeInnerMethod = innerTaskType.GetMethod(nameof(ExecuteInner), BindingFlags.Instance | BindingFlags.NonPublic);
            var result = (bool)executeInnerMethod.Invoke(innerTask, new object[0]);

            foreach (var propertyPair in outputPropertiesMap)
            {
                propertyPair.outerProperty.SetValue(this, propertyPair.innerProperty.GetValue(innerTask));
            }

            return result;
#else
            // On .NET Framework (on Windows), we find native binaries by adding them to our PATH.
            //if (this.UnmanagedDllDirectory != null)
            //{
            //    string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            //    string[] searchPaths = pathEnvVar.Split(Path.PathSeparator);
            //    if (!searchPaths.Contains(this.UnmanagedDllDirectory, StringComparer.OrdinalIgnoreCase))
            //    {
            //        pathEnvVar += Path.PathSeparator + this.UnmanagedDllDirectory;
            //        Environment.SetEnvironmentVariable("PATH", pathEnvVar);
            //    }
            //}

            return ExecuteInner();
#endif
        }

        protected abstract bool ExecuteInner();

#if NETCOREAPP2_1
        class CustomAssemblyLoader : AssemblyLoadContext
        {
            readonly ContextAwareTask loaderTask;

            internal CustomAssemblyLoader(ContextAwareTask loaderTask)
            {
                this.loaderTask = loaderTask;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                var assemblyPath = Path.Combine(loaderTask.ManagedDllDirectory, assemblyName.Name) + ".dll";
                if (File.Exists(assemblyPath))
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return Default.LoadFromAssemblyName(assemblyName);
            }

            //protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            //{
            //    string unmanagedDllPath = Directory.EnumerateFiles(
            //        this.loaderTask.UnmanagedDllDirectory,
            //        $"{unmanagedDllName}.*").Concat(
            //            Directory.EnumerateFiles(
            //                this.loaderTask.UnmanagedDllDirectory,
            //                $"lib{unmanagedDllName}.*"))
            //        .FirstOrDefault();
            //    if (unmanagedDllPath != null)
            //    {
            //        return this.LoadUnmanagedDllFromPath(unmanagedDllPath);
            //    }

            //    return base.LoadUnmanagedDll(unmanagedDllName);
            //}
        }
#endif
    }
}
