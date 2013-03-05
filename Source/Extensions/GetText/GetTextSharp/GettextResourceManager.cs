using System;
using System.Collections.Specialized;
using System.Configuration;

namespace PHP.Library.GetText.GetTextSharp
{
    public class GettextResourceManager : FileBasedResourceManager
    {
        private const string defaultFileFormat = "{{culture}}\\{{resource}}.po";
        private const string defaultPath = "";
        public override Type ResourceSetType
        {
            get
            {
                return typeof(GettextResourceSet);
            }
        }
        public GettextResourceManager(string name, string path, string fileformat)
            : base(name, path, fileformat)
        {
        }
        public GettextResourceManager(string name)
            : base(name, "", "{{culture}}\\{{resource}}.po")
        {
        }
        public bool LoadConfiguration(string section)
        {
            NameValueCollection nameValueCollection = ConfigurationManager.GetSection(section) as NameValueCollection;
            if (nameValueCollection == null)
            {
                return false;
            }
            base.FileFormat = (nameValueCollection["fileformat"] ?? base.FileFormat);
            base.Path = (nameValueCollection["path"] ?? base.Path);
            return true;
        }
        public static FileBasedResourceManager CreateFromConfiguration(string name, string section)
        {
            return GettextResourceManager.CreateFromConfiguration(name, section, "{{culture}}\\{{resource}}.po", "");
        }
        public static FileBasedResourceManager CreateFromConfiguration(string name, string section, string fallbackFileFormat, string fallbackPath)
        {
            NameValueCollection nameValueCollection = ConfigurationManager.GetSection(section) as NameValueCollection;
            string fileformat = null;
            string path = null;
            if (nameValueCollection == null)
            {
                fileformat = fallbackFileFormat;
                path = fallbackPath;
            }
            else
            {
                fileformat = (nameValueCollection["fileformat"] ?? fallbackFileFormat);
                path = (nameValueCollection["path"] ?? fallbackPath);
            }
            return new FileBasedResourceManager(name, path, fileformat);
        }
    }
}
