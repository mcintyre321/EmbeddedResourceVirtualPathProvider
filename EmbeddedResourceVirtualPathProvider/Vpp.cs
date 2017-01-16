using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmbeddedResourceVirtualPathProvider
{
    public class Vpp : VirtualPathProvider, IEnumerable
    {
		readonly Dictionary<string, Dictionary<string, List<EmbeddedResource>>> resources = new Dictionary<string, Dictionary<string, List<EmbeddedResource>>>();

		Regex pathPattern = new Regex(@"^(?<directory>.*?)?\.(?<filename>[^.]*([\.-](?<version>[0-9]{1,5}\.[0-9]{1,5}\.[0-9]{1,5})(\.min)?)?\.[^.]*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public Vpp(params Assembly[] assemblies)
        {            
            UseResource = er => true;
            UseLocalIfAvailable = resource => true;
            CacheControl = er => null;
			GetPath = resourcePath => DefaultPathFunction(resourcePath);

			Array.ForEach(assemblies, a => Add(a));
		}

        public Func<EmbeddedResource, bool> UseResource { get; set; }
        public Func<EmbeddedResource, bool> UseLocalIfAvailable { get; set; }
        public Func<EmbeddedResource, EmbeddedResourceCacheControl> CacheControl { get; set; }
		public Func<string, EmbeddedResourcePath> GetPath { get; set; }

		public Dictionary<string, Dictionary<string, List<EmbeddedResource>>> Resources { get { return resources; } }

		private EmbeddedResourcePath DefaultPathFunction(string resourcePath)
		{
			Match match = pathPattern.Match(resourcePath);
			if (match.Success)
			{
				return new EmbeddedResourcePath()
				{
					Directory = match.Groups["directory"].Value,
					Filename = match.Groups["filename"].Value
				};
			}

			return null;
		}

        public void Add(Assembly assembly, string projectSourcePath = null)
        {
			var assemblyName = assembly.GetName().Name;
			
            foreach (var resourcePath in assembly.GetManifestResourceNames().Where(r => r.StartsWith(assemblyName)))
            {
				EmbeddedResourcePath path = GetPath(resourcePath.Substring(assemblyName.Length + 1));
				if (path != null)
				{
					Dictionary<string, List<EmbeddedResource>> directoryResources;
					string directoryName = path.Directory.ToUpperInvariant();
					string filename = path.Filename.ToUpperInvariant();

					if (!resources.TryGetValue(directoryName, out directoryResources))
					{
						directoryResources = new Dictionary<string, List<EmbeddedResource>>();
						resources.Add(directoryName, directoryResources);
					}

					if (!directoryResources.ContainsKey(filename))
					{
						directoryResources[filename] = new List<EmbeddedResource>();
					}
					directoryResources[filename].Insert(0, new EmbeddedResource(assembly, resourcePath, projectSourcePath));
				}
            }
        }
 
        public override bool FileExists(string virtualPath)
        {
            return (base.FileExists(virtualPath) || GetResourceFromVirtualPath(virtualPath) != null);
        }

		public override VirtualDirectory GetDirectory(string virtualDir)
		{
			string key = virtualDir.Replace('/', '.').TrimStart('~', '.').TrimEnd('.').ToUpperInvariant();

			Dictionary<string, List<EmbeddedResource>> directoryResources;
			
			if (resources.TryGetValue(key, out directoryResources))
			{
				return new EmbeddedResourceVirtualDirectory(virtualDir, directoryResources, CacheControl);
			}

			return base.GetDirectory(virtualDir);
		}

		public override bool DirectoryExists(string virtualDir)
		{
			string key = virtualDir.Replace('/', '.').TrimStart('~', '.').TrimEnd('.').ToUpperInvariant();
			if (resources.ContainsKey(key))
			{
				return true;
			}

			return base.DirectoryExists(virtualDir);
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

			Match pathMatch = pathPattern.Match(path.Replace('/', '.').ToUpperInvariant());
			if (pathMatch.Success)
			{
				string directory = pathMatch.Groups["directory"].Value;
				string filename = pathMatch.Groups["filename"].Value;

				if (resources.ContainsKey(directory))
				{
					var directoryResources = resources[directory];
					if (directoryResources.ContainsKey(filename))
					{
						var resource = directoryResources[filename].FirstOrDefault(UseResource);
						if (resource != null && !ShouldUsePrevious(virtualPath, resource))
						{
							return resource;
						}
					}
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