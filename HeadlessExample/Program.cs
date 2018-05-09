// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Copyright © 2018 Trimble Solutions Corporation. Trimble Solutions Corporation is a Trimble Company">
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace HeadlessExample
{
    using System;
    using System.IO;
    using Tekla.Structures.Model;
    using Tekla.Structures.Service;

    class Program
    {
        static void Main(string[] args)
        {
            // TeklaStructuresService needs to know the location of the TS binaries.
            using (var service = new TeklaStructuresService(
                new DirectoryInfo(@"C:\Program Files\Tekla Structures\2018\nt\bin")))
            {
                service.Initialize(
                    new DirectoryInfo(@"D:\TeklaStructuresModels\ESM"),
                    new FileInfo(@"C:\ProgramData\Tekla Structures\2018\Environments\default\env_Default_environment.ini"),
                    new FileInfo(@"C:\ProgramData\Tekla Structures\2018\Environments\default\role_All.ini"),
                    "Viewer");

                // If you setup the licenses, environments and roles in the .ini files, you don't need to provide them:
                //service.Initialize(
                //    new DirectoryInfo(@"D:\TeklaStructuresModels\ESM"));

                //!IMPORTANT NOTICE!
                //!IMPORTANT NOTICE!
                //!IMPORTANT NOTICE!
                //You cannot run the above code when in debugger. 
                //You can add a ReadKey here, so you can attach debugger to be able to debug your own code.
                //Console.ReadKey();
                
                Console.WriteLine("ProjectInfo.Name        : " + new Model().GetProjectInfo().Name);
                Console.WriteLine("ProjectInfo.Description : " + new Model().GetProjectInfo().Description);
            } // service must be disposed so that a clean exit is done. Once disposed, it cannot be used again during the process lifetime.
        }
    }
}
