using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider
{
	class EmbeddedResourceVirtualDirectory : VirtualDirectory
	{
		Dictionary<string, List<EmbeddedResource>> _resources;
		Func<EmbeddedResource, EmbeddedResourceCacheControl> _cacheControl;

		public EmbeddedResourceVirtualDirectory(string virtualPath, Dictionary<string, List<EmbeddedResource>> resources, Func<EmbeddedResource, EmbeddedResourceCacheControl> cacheControl) : base(virtualPath)
		{
			_resources = resources;
			_cacheControl = cacheControl;
		}

		public override IEnumerable Children
		{
			get
			{
				return Files;
			}
		}

		public override IEnumerable Directories
		{
			get
			{
				return new List<VirtualDirectory>();
			}
		}

		public override IEnumerable Files
		{
			get
			{
				return _resources.Select(resource => new EmbeddedResourceVirtualFile(System.IO.Path.Combine(this.VirtualPath, resource.Key), resource.Value.FirstOrDefault(), _cacheControl(resource.Value.FirstOrDefault())));				
			}
		}
	}
}
