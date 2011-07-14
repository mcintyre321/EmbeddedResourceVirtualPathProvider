using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using EmbeddedResourceVirtualPathProvider;
using TestResourceLibrary;

namespace TestWebProject.App_Code
{
    namespace TestWebProject
    {
        public class Global 
        {
            public static void AppInitialize()
            {
                HostingEnvironment.RegisterVirtualPathProvider(new Vpp()
                {
                    {typeof(Marker).Assembly, @"..\TestResourceLibrary"},

                });
            }
        }
    }
}