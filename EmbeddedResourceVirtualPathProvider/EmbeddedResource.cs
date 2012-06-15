using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Caching;

namespace EmbeddedResourceVirtualPathProvider
{
    class EmbeddedResource
    {
        public EmbeddedResource(Assembly assembly, string resourcePath, string projectSourcePath)
        {
            if (!string.IsNullOrWhiteSpace(projectSourcePath))
            {
                var filename = GetFileNameFromProjectSourceDirectory(assembly, resourcePath, projectSourcePath);

                if (filename != null) //means that the source file was found, or a copy was in the web apps folders
                {
                    GetCacheDependency = (utcStart) => new CacheDependency(filename, utcStart);
                    GetStream = () => File.OpenRead(filename);
                    return;
                }
            }
            GetCacheDependency = (utcStart) => new CacheDependency(assembly.Location);
            GetStream = () => assembly.GetManifestResourceStream(resourcePath);
        }

        public Func<Stream> GetStream { get; private set; }
        public Func<DateTime, CacheDependency> GetCacheDependency { get; private set; }

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
#endif
                Logger.LogWarning("Error reading source files", ex);
                return null;
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