using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using System.Linq;

namespace EmbeddedResourceVirtualPathProvider
{
    public class Vpp : VirtualPathProvider, IEnumerable
    {
        readonly IDictionary<string, List<EmbeddedResource>> resources = new Dictionary<string, List<EmbeddedResource>>();

        public Vpp(params Assembly[] assemblies)
        {
            Array.ForEach(assemblies, a => Add(a));
            UseResource = er => true;
            UseLocalIfAvailable = resource => true;
            CacheControl = er => null;
        }

        public Func<EmbeddedResource, bool> UseResource { get; set; }
        public Func<EmbeddedResource, bool> UseLocalIfAvailable { get; set; }
        public Func<EmbeddedResource, EmbeddedResourceCacheControl> CacheControl { get; set; }
        public IDictionary<string, List<EmbeddedResource>>  Resources { get { return resources; } }

        public void Add(Assembly assembly, string projectSourcePath = null)
        {
            var assemblyName = assembly.GetName().Name;
            foreach (var resourcePath in assembly.GetManifestResourceNames().Where(r => r.StartsWith(assemblyName)))
            {
                var key = resourcePath.ToUpperInvariant().Substring(assemblyName.Length).TrimStart('.');
                if (!resources.ContainsKey(key))
                    resources[key] = new List<EmbeddedResource>();
                resources[key].Insert(0, new EmbeddedResource(assembly, resourcePath, projectSourcePath));
            }
        }
 
        public override bool FileExists(string virtualPath)
        {
            return (base.FileExists(virtualPath) || GetResourceFromVirtualPath(virtualPath) != null);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            //if (base.FileExists(virtualPath)) return base.GetFile(virtualPath);
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
                return new EmbeddedResourceVirtualFile(virtualPath, resource, CacheControl(resource));
            return base.GetFile(virtualPath);
        }

        public override string CombineVirtualPaths(string basePath, string relativePath)
        {
            var combineVirtualPaths = base.CombineVirtualPaths(basePath, relativePath);
            return combineVirtualPaths;
        }
        public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
        {
            var fileHash = base.GetFileHash(virtualPath, virtualPathDependencies);
            return fileHash;
        }

        public override string GetCacheKey(string virtualPath)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
            {
                return (virtualPath + resource.AssemblyName + resource.AssemblyLastModified.Ticks).GetHashCode().ToString();
            }
            return base.GetCacheKey(virtualPath);
        }

        public bool IsInt(string c)
        {
            int outChar;
            return int.TryParse(c, out outChar);
        }

        public EmbeddedResource GetResourceFromVirtualPath(string virtualPath)
        {
            var path = VirtualPathUtility.ToAppRelative(virtualPath).TrimStart('~', '/');
            var index = path.LastIndexOf("/");
            if (index != -1)
            {
                var folder = path.Substring(0, index); //embedded resources with "-"in their folder names are stored as "_".
                var folderItems = folder.Split('/');
                List<string> result = new List<string>();
                foreach (var item in folderItems)
                {
                    var resultFolder = item;
                    resultFolder = resultFolder.Replace("-", "_"); //replace - with underscore

                    var outputFolder = "";
                    var outs = "";
                    for (var i = 0; i < resultFolder.Length; i++)
                    {
                        if (i == 0 && IsInt(resultFolder[i].ToString())) //if the first character is a int, then prefix
                            outs += "_";

                        outs += resultFolder[i];

                        //if any character follows a dot with a int, prefix with an underscore
                        if (resultFolder[i] == '.')
                        {
                            //get the next one
                            if (IsInt(resultFolder.Substring(i + 1, 1)))
                                outs += "_";
                        }
                    }
                    resultFolder = outs;
                    result.Add(resultFolder);
                }
                folder = string.Join(".", result);

                path = folder + path.Substring(index);
            }
            var cleanedPath = path.Replace('/', '.');
            var key = (cleanedPath).ToUpperInvariant();
            if (resources.ContainsKey(key))
            {
                var resource = resources[key].FirstOrDefault(UseResource);
                if (resource != null && !ShouldUsePrevious(virtualPath, resource))
                {
                    return resource;
                }
            }
            return null;
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
            {
                return resource.GetCacheDependency(utcStart);
            }

            var embeddedResourceDependencies = virtualPathDependencies.OfType<string>()
                .Select(x => new { path = x, resource = GetResourceFromVirtualPath(x) })
                .Where(x => x.resource != null)
                .ToList();

            if (embeddedResourceDependencies.Any())
            {
                virtualPathDependencies = virtualPathDependencies.OfType<string>()
                    .Except(embeddedResourceDependencies.Select(v => v.path))
                    .Concat(embeddedResourceDependencies.Select(v => $"/bin/{v.resource.AssemblyName}").Distinct());
            }

            if (DirectoryExists(virtualPath) || FileExists(virtualPath))
            {
                return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
            }

            return null;
        }

        private bool ShouldUsePrevious(string virtualPath, EmbeddedResource resource)
        {
            return base.FileExists(virtualPath) && UseLocalIfAvailable(resource);
        }

        
        //public override string GetCacheKey(string virtualPath)
        //{
        //    var resource = GetResourceFromVirtualPath(virtualPath);
        //    if (resource != null) return virtualPath + "blah";
        //    return base.GetCacheKey(virtualPath);
        //}

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException("Only got this so that we can use object collection initializer syntax");
        }
    }
}