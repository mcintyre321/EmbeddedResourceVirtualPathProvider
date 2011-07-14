using System.Web.Hosting;
using EmbeddedResourceVirtualPathProvider;
using TestResourceLibrary;

namespace TestWebProject.App_Code
{
    public class RegisterVirtualPathProvider
    {
        public static void AppInitialize()
        {
            HostingEnvironment.RegisterVirtualPathProvider(new EmbeddedResourceVirtualPathProvider.Vpp()
            {
                {typeof (Marker).Assembly, @"..\TestResourceLibrary"},

            });
        }
    }
}