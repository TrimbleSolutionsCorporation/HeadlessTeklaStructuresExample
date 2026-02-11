namespace HeadlessExample
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintUsage();
                return;
            }

            var modelPath = args[0];
            var tsVersion = args.Length > 1 ? args[1] : "2025.0";
            var envIniPath = args.Length > 2 ? args[2] : null;
            var roleIniPath = args.Length > 3 ? args[3] : null;
            var license = args.Length > 4 ? args[4] : "Diamond";

            if (!Directory.Exists(modelPath))
            {
                Console.WriteLine($"Error: Model path not found: {modelPath}");
                return;
            }

            var binDir = $@"C:\Program Files\Tekla Structures\{tsVersion}\bin";
            if (!Directory.Exists(binDir))
            {
                Console.WriteLine($"Error: Tekla bin directory not found: {binDir}");
                Console.WriteLine($"Is Tekla Structures {tsVersion} installed?");
                return;
            }

            ConfigureHeadlessIniFiles(binDir);

            // Register assembly resolver BEFORE any Tekla/Trimble types are touched.
            // All code that references those types lives in Run(), which the JIT won't
            // compile until the call below — so the resolver is already in place.
            AppDomain.CurrentDomain.AssemblyResolve += (s, a) => ResolveFromBin(a, binDir);

            Run(modelPath, tsVersion, envIniPath, roleIniPath, license, binDir);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Run(string modelPath, string tsVersion,
            string envIniPath, string roleIniPath, string license, string binDir)
        {
            // Step 1: Authenticate via Trimble Identity (requires prior GUI sign-in)
            string idToken = null;
            string accessToken = null;
            Console.WriteLine("Authenticating via Trimble Identity...");
            try
            {
                using (var authManager = CreateAuthManager(tsVersion))
                {
                    var authResult = authManager.Authentication
                        .RefreshTokenAsync(System.Threading.CancellationToken.None, true)
                        .GetAwaiter().GetResult();

                    if (authResult.Result == Trimble.Auth.AuthenticationResult.SUCCESS)
                    {
                        var token = authManager.Authentication.Token;
                        idToken = token.TrimbleIdToken;
                        accessToken = token.TrimbleAccessToken;
                        Console.WriteLine("  Authenticated successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"  Authentication failed: {authResult.Result}");
                        Console.WriteLine("  Make sure you have signed into Tekla Structures at least once.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Authentication error: {ex.Message}");
                return;
            }

            // Step 2: Start TeklaStructuresService with OAuth tokens
            using (var service = CreateService(binDir, envIniPath, roleIniPath))
            {
                Console.WriteLine($"Initializing Tekla service (model: {modelPath})...");
                service.Initialize(
                    new DirectoryInfo(modelPath),
                    license,
                    idToken,
                    accessToken,
                    false,
                    null);

                // Step 3: Wait for license activation and query the model
                Console.WriteLine("Waiting for license...");
                var model = new Tekla.Structures.Model.Model();
                for (int attempt = 1; attempt <= 12; attempt++)
                {
                    System.Threading.Thread.Sleep(5000);
                    var objects = model.GetModelObjectSelector().GetAllObjects();
                    int count = 0;
                    while (objects.MoveNext()) count++;

                    if (count > 0)
                    {
                        Console.WriteLine($"License active after {attempt * 5}s.");
                        Console.WriteLine($"ProjectInfo.Name: {model.GetProjectInfo().Name}");

                        objects = model.GetModelObjectSelector().GetAllObjects();
                        int shown = 0;
                        while (objects.MoveNext())
                        {
                            shown++;
                            if (shown <= 5)
                            {
                                var obj = objects.Current;
                                Console.WriteLine($"  [{shown}] {obj.GetType().Name} Id={obj.Identifier.ID}");
                            }
                        }
                        Console.WriteLine($"Total: {count} objects");
                        break;
                    }

                    Console.WriteLine($"  Attempt {attempt}/12 ({attempt * 5}s): waiting...");
                }

                Console.WriteLine("Done.");
            }
        }

        private static Trimble.Auth.AuthManager CreateAuthManager(string tsVersion)
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Trimble", "Tekla Structures", "AuthCache");
            Directory.CreateDirectory(cacheDir);

            var config = new Trimble.Auth.AuthManagerConfiguration(
                Path.Combine(cacheDir, "headless_auth.cache"),
                Microsoft.Win32.Registry.CurrentUser,
                $@"Software\Trimble\Tekla Structures\{tsVersion}\Session",
                null,
                false,
                Trimble.Auth.AuthManagerConfiguration.Environment.PRODUCTION,
                "en");

            return new Trimble.Auth.AuthManager(config);
        }

        private static Tekla.Structures.Service.TeklaStructuresService CreateService(
            string binDir, string envIniPath, string roleIniPath)
        {
            return new Tekla.Structures.Service.TeklaStructuresService(
                new DirectoryInfo(binDir),
                "ENGLISH",
                envIniPath != null ? new FileInfo(envIniPath) : null,
                roleIniPath != null ? new FileInfo(roleIniPath) : null);
        }

        private static void ConfigureHeadlessIniFiles(string binDir)
        {
            var runPath = Path.Combine(Path.GetTempPath(), "HeadlessExample_RunPath");
            var overrideIniFile = Path.Combine(runPath, "TeklaStructures.ini");
            Directory.CreateDirectory(runPath);
            File.Copy(Path.Combine(binDir, "TeklaStructures.ini"), overrideIniFile, true);
            Environment.SetEnvironmentVariable("TS_OVERRIDE_INI_FILE", overrideIniFile);
        }

        private static Assembly ResolveFromBin(ResolveEventArgs args, string binDir)
        {
            var name = new AssemblyName(args.Name);
            var path = Path.Combine(binDir, name.Name + ".dll");

            if (File.Exists(path))
            {
                return Assembly.LoadFile(path);
            }

            return null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("HeadlessExample - Run Tekla Structures headlessly with subscription licensing");
            Console.WriteLine();
            Console.WriteLine("Usage: HeadlessExample.exe <modelPath> [teklaVersion] [envIniPath] [roleIniPath] [license]");
            Console.WriteLine();
            Console.WriteLine("  modelPath    Full path to Tekla model folder (required)");
            Console.WriteLine("  teklaVersion Tekla version, e.g. \"2025.0\" (default: 2025.0)");
            Console.WriteLine("  envIniPath   Full path to environment .ini file (optional)");
            Console.WriteLine("  roleIniPath  Full path to role .ini file (optional)");
            Console.WriteLine("  license      License type: Diamond, Premium, etc. (default: Diamond)");
            Console.WriteLine();
            Console.WriteLine("Prerequisites:");
            Console.WriteLine("  - Tekla Structures must be installed");
            Console.WriteLine("  - You must have signed into Tekla Structures GUI at least once");
        }
    }
}
