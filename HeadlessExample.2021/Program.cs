// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Copyright © 2022 Trimble Solutions Corporation. Trimble Solutions Corporation is a Trimble Company">
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace HeadlessExample
{
    using System;
    using System.IO;
    using System.Reflection;

    using Tekla.Structures.Model;
    using Tekla.Structures.Service;

    class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">additional version field: Ex: HeadlessExample.2021.exe 2021.0 for running example against 2021 version of ts</param>
        static void Main(string[] args)
        {
            // default ts to use. change to 2021 as example. 
            var tsVersion = "2022.0";
            if (args.Length != 0)
            {
                tsVersion = args[0];
            }

            var binDir = $@"C:\Program Files\Tekla Structures\{tsVersion}\bin";
            if (tsVersion.Equals("2021.0"))
            {
                // backwards compitbility
                binDir = $@"C:\Program Files\Tekla Structures\{tsVersion}\nt\bin";
            }

            ConfigureHeadlessIniFiles(binDir);

            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => CurrentDomainAssemblyResolve(a, binDir);
            // TeklaStructuresService needs to know the location of the TS binaries.
            // additionally we need to set a resolver to load dlls from binary folder
            using (var service = new TeklaStructuresService(
                new DirectoryInfo(binDir),
                "ENGLISH",
                new FileInfo($@"C:\ProgramData\Trimble\Tekla Structures\{tsVersion}\Environments\default\env_Default_environment.ini"),
                new FileInfo($@"C:\ProgramData\Trimble\Tekla Structures\{tsVersion}\Environments\default\role_Steel_Detailer.ini")))
            {
                // change for model path
                var modelPath = @"C:\TeklaStructuresModels\New model";
                // see Initialize for role, license and identity
                service.Initialize(new DirectoryInfo(modelPath));

                //!IMPORTANT NOTICE!
                //!IMPORTANT NOTICE!
                //!IMPORTANT NOTICE!
                //You cannot run the above code when in debugger. 
                //You can add a ReadKey here, so you can attach debugger to be able to debug your own code.
                //Console.ReadKey();

                // at this point we can call TeklaOpen api to do TS operations
                Console.WriteLine("ProjectInfo.Name        : " + new Model().GetProjectInfo().Name);
                Console.WriteLine("ProjectInfo.Description : " + new Model().GetProjectInfo().Description);
            } // service must be disposed so that a clean exit is done. Once disposed, it cannot be used again during the process lifetime.
        }

        /// <summary>
        /// Helpers function to set need patching for licensing or any other things required
        /// </summary>
        /// <param name="binDir"></param>
        private static void ConfigureHeadlessIniFiles(string binDir)
        {
            var licenseServer = "27001@yourserver";
            var runPath = @"C:\TeklaStructuresModels\RunPath";
            var overrideIniFile = Path.Combine(runPath, "TeklaStructures.ini");
            Directory.CreateDirectory(runPath);
            // Always make a copy of a main the ini file so we dont break TS installation.
            File.Copy(Path.Combine(binDir, "TeklaStructures.ini"), overrideIniFile, true);
            File.AppendAllText(overrideIniFile, $"\r\nset XS_LICENSE_SERVER_HOST={licenseServer}\r\n");
            File.AppendAllText(overrideIniFile, $"set XS_DEFAULT_LICENSE=Full\r\n");
            
            var envVar = Environment.GetEnvironmentVariable("TS_OVERRIDE_INI_FILE");
            if (envVar == null || !envVar.Equals(overrideIniFile))
            {
                throw new ArgumentException($"Please set TS_OVERRIDE_INI_FILE, before exec the program: set TS_OVERRIDE_INI_FILE={overrideIniFile}");
            }
        }

        /// <summary>
        /// assembly resolver - for loading dlls from bin without copying dlls into exectution path
        /// </summary>
        /// <param name="args">event args</param>
        /// <param name="binDir">ts binary folder</param>
        /// <returns>assembly to load</returns>
        private static System.Reflection.Assembly CurrentDomainAssemblyResolve(ResolveEventArgs args, string binDir)
        {
            var requestedAssembly = new AssemblyName(args.Name);

            if (File.Exists(Path.Combine(binDir, requestedAssembly.Name + ".dll")))
            {
                return System.Reflection.Assembly.LoadFile(Path.Combine(binDir, requestedAssembly.Name + ".dll"));
            }

            return null;            
        }
    }
}
