using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Caching;

namespace EmbeddedResourceVirtualPathProvider
{
    public class EmbeddedResource
    {
        public EmbeddedResource(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            this.AssemblyName = assembly.GetName().Name;
            System.IO.FileInfo fileInfo = new System.IO.FileInfo(assembly.Location);
            AssemblyLastModified = fileInfo.LastWriteTime;
            this.ResourcePath = resourcePath;
            if (!string.IsNullOrWhiteSpace(projectSourcePath))
            {
                Filename = GetFileNameFromProjectSourceDirectory(assembly, resourcePath, projectSourcePath);

                if (Filename != null) //means that the source file was found, or a copy was in the web apps folders
                {
                    GetCacheDependency = (utcStart) => new CacheDependency(Filename, utcStart);
                    GetStream = () => File.OpenRead(Filename);
                    return;
                }
            }
            GetCacheDependency = (utcStart) => new CacheDependency(assembly.Location);
            GetStream = () => assembly.GetManifestResourceStream(resourcePath);
        }

        public string Filename { get; private set; }

        public DateTime AssemblyLastModified { get; private set; }

        public string ResourcePath { get; private set; }

        public Func<Stream> GetStream { get; private set; }
        public Func<DateTime, CacheDependency> GetCacheDependency { get; private set; }

        public string AssemblyName { get; private set; }

        string GetFileNameFromProjectSourceDirectory(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            try
            {
                if (!Path.IsPathRooted(projectSourcePath))
                {
                    projectSourcePath =
                        new DirectoryInfo((Path.Combine(HttpRuntime.AppDomainAppPath, projectSourcePath))).FullName;
                }
                var fileName = Path.Combine(projectSourcePath,
                                            resourcePath.Substring(assembly.GetName().Name.Length + 1).Replace('.', '\\'));


                return GetFileName(fileName);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#else
                Logger.LogWarning("Error reading source files", ex);
                return null;
#endif
            }
        }

        string GetFileName(string possibleFileName)
        {
            var indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            while (indexOfLastSlash > -1)
            {
                if (File.Exists(possibleFileName)) return possibleFileName;
                possibleFileName = ReplaceChar(possibleFileName, indexOfLastSlash, '.');
                indexOfLastSlash = possibleFileName.LastIndexOf('\\');
            }
            return null;
        }


        string ReplaceChar(string text, int index, char charToUse)
        {
            char[] buffer = text.ToCharArray();
            buffer[index] = charToUse;
            return new string(buffer);
        }
    }
}