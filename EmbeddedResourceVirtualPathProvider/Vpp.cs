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
        readonly IDictionary<string, EmbeddedResource> resources = new Dictionary<string, EmbeddedResource>();

        public Vpp()
        {
        }

        public void Add(Assembly assembly, string projectSourcePath = null)
        {
            var assemblyName = assembly.GetName().Name;
            foreach (var resourcePath in assembly.GetManifestResourceNames().Where(r => r.StartsWith(assemblyName)))
            {
                var key = resourcePath.ToUpperInvariant().Substring(assemblyName.Length).TrimStart('.');
                resources[key] = new EmbeddedResource(assembly, resourcePath, projectSourcePath);
            }
        }
 
        public override bool FileExists(string virtualPath)
        {
            return (base.FileExists(virtualPath) || GetResourceFromVirtualPath(virtualPath) != null);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (base.FileExists(virtualPath)) return base.GetFile(virtualPath);
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null)
                return new EmbeddedResourceVirtualFile(virtualPath, resource);
            return base.GetFile(virtualPath);
        }

        EmbeddedResource GetResourceFromVirtualPath(string virtualPath)
        {
            var cleanedPath = VirtualPathUtility.ToAppRelative(virtualPath).TrimStart('~', '/').Replace('/', '.');
            var key = (cleanedPath).ToUpperInvariant();
            if (resources.ContainsKey(key)) return resources[key];
            return null;
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies, DateTime utcStart)
        {
            var resource = GetResourceFromVirtualPath(virtualPath);
            if (resource != null) return resource.GetCacheDependency(utcStart);
            
            return base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException("Only got this so that we can use object collection initializer syntax");
        }
    }
}