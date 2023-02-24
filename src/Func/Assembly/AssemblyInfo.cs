using System.Reflection;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        /// <summary>
        /// Returns a dictionary with the Assembly info
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetAssemblyInformation(string File)
        {
            Assembly assembly = Assembly.LoadFrom(File);
            Dictionary<string, string> assemblyInfo = new Dictionary<string, string>();

            var name = assembly.GetName().Name.ToString();
            assemblyInfo.Add("Name", name);

            var fullName = assembly.GetName().FullName.ToString();
            assemblyInfo.Add("FullName", fullName);

            var version = assembly.GetName().Version.ToString();
            assemblyInfo.Add("Version", version);

            var versionMajor = assembly.GetName().Version.Major.ToString();
            assemblyInfo.Add("Version.Major", versionMajor);

            var versionRevision = assembly.GetName().Version.Revision.ToString();
            assemblyInfo.Add("Version.Revision", versionRevision);

            var versionMinor = assembly.GetName().Version.Minor.ToString();
            assemblyInfo.Add("Version.Minor", versionMinor);

            var description = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
            .OfType<AssemblyDescriptionAttribute>()
            .FirstOrDefault()?
            .Description ?? "";
            assemblyInfo.Add("Description", description);

            return assemblyInfo;
        }
    }
}