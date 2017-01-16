using System.Linq;
using System.Reflection;

//[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(NugetTestWebProject.EmbeddedResourceVirtualPathProviderStart), "Start")]

namespace TestWebProject
{
    public static class EmbeddedResourceVirtualPathProviderStart
    {
        public static void Start()
        {
			//By default, we scan all non system assemblies for embedded resources
            var assemblies = System.Web.Compilation.BuildManager.GetReferencedAssemblies()
                .Cast<Assembly>()
                .Where(a => a.GetName().Name.StartsWith("System") == false);

			var vpp = new EmbeddedResourceVirtualPathProvider.Vpp(assemblies.ToArray());

			System.Web.Hosting.HostingEnvironment.RegisterVirtualPathProvider(vpp);           
        }
    }
}