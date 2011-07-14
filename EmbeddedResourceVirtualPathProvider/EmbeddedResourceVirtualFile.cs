using System.IO;
using System.Web.Hosting;

namespace EmbeddedResourceVirtualPathProvider
{
    class EmbeddedResourceVirtualFile : VirtualFile
    {
        readonly EmbeddedResource embedded;

        public EmbeddedResourceVirtualFile(string virtualPath, EmbeddedResource embedded)
            : base(virtualPath)
        {
            this.embedded = embedded;
        }

        public override Stream Open()
        {
            return embedded.GetStream();
        }
    }
}