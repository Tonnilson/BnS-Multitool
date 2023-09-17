using System;

namespace BnS_Multitool.Extensions
{
    [AttributeUsage(AttributeTargets.All)]
    public class AgonTheme : Attribute
    {
        public string Name { get; private set; }
        public AgonTheme(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class WorryTheme : Attribute
    {
        public string Name { get; private set; }
        public WorryTheme(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class GameIdAttribute : Attribute
    {
        public string Name { get; private set; }
        public GameIdAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class LauncherAddrAttribute : Attribute
    {
        public string Name { get; private set; }

        public LauncherAddrAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class CDNAttribute : Attribute
    {
        public string Name { get; private set; }

        public CDNAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class GameIPAddressAttribute : Attribute
    {
        public string Name { get; private set; }
        public GameIPAddressAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class AppIdAttribute : Attribute
    {
        public string AppId { get; private set; }
        public AppIdAttribute(string name) => AppId = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class CligateAttribute : Attribute
    {
        public string Name { get; private set; }
        public CligateAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class RegionAttribute : Attribute
    {
        public string Name { get; private set;}
        public RegionAttribute(string name) => Name = name;
    }

    [AttributeUsage(AttributeTargets.All)]
    public class RegistryPathAttribute : Attribute
    {
        public string Path { get; private set; }
        public RegistryPathAttribute(string path) => Path = path;
    }
}
