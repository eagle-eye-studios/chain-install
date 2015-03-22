using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ChainInstall
{
    class Program
    {
        const string ISS_PATH = "iss";
        const string ISS_GEN_SUFFIX = "_generated";
        const string SETUP_PATH = "files";
        const string LOG_FILE = "iss.log";

        static string WorkingDir { get; set; }
        static OpMode Mode { get; set; }

        static Dictionary<string, string> fileMappings;
        static Dictionary<string, string> variables;
        static string launchFile = "";

        static void Main(string[] args)
        {
            Console.WriteLine("ChainInstall v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " © 2015 Eagle Eye Studios. https://github.com/eagle-eye-studios/chain_install");
            foreach (String arg in args)
            {
                Console.WriteLine(arg);
                if (arg.Equals("/r"))
                    Mode = OpMode.Remove;
                if (arg.StartsWith("/wdir"))
                    SetWDir(arg);
            }

            fileMappings = new Dictionary<string, string>();
            variables = new Dictionary<string, string>();

            if (Mode == OpMode.Remove)
                RemovePackages();
            else
                InstallPackages();

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Have a nice day/night!");
            Console.ReadKey();
        }

        static void InstallPackages()
        {
            Console.WriteLine("Selected Mode: INSTALL");
            if (!ReadSettings())
                return;
            if (!GenerateIssFiles())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Generation of ISS files has failed!");
                Console.ResetColor();
                return;
            }
            if (!DoChainCall(false))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Chain installation has failed.");
                Console.ResetColor();
                return;
            }
            CleanUp();
            LaunchFinishFile();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Install chain finished.");
        }

        static void LaunchFinishFile()
        {
            if (launchFile != String.Empty)
                Process.Start(launchFile);
        }

        static void RemovePackages()
        {
            Console.WriteLine("Selected Mode: REMOVE");
            if (!ReadSettings())
                return;
            if (!DoChainCall(true))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Chain removal has failed.");
                Console.ResetColor();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Removal chain finished.");
        }

        static void SetWDir(string arg)
        {
            WorkingDir = arg.Remove(0, 5);
            Console.WriteLine("Working Dir set to" + WorkingDir);
        }

        static bool ReadSettings()
        {
            FileIniDataParser iniParser = new FileIniDataParser();
            try
            {
                IniData data = iniParser.ReadFile(appendToExecDir("CHAIN_INSTALL.INI"));
                foreach (var key in data.Sections.GetSectionData("mappings").Keys)
                    fileMappings.Add(key.KeyName, key.Value);
                foreach (var key in data.Sections.GetSectionData("variables").Keys)
                    variables.Add(key.KeyName, key.Value);
                if (data.Sections.ContainsSection("other"))
                {
                    if (data.Sections.GetSectionData("other").Keys.ContainsKey("launchWhenInstalled"))
                    {
                        launchFile = data.Sections.GetSectionData("other").Keys.GetKeyData("launchWhenInstalled").Value;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Could not reade config: " + ex.Message);
                Console.ResetColor();
                return false;
            }
        }

        static bool GenerateIssFiles()
        {
            Console.WriteLine("Generating ISS files...");
            foreach (string issFileName in fileMappings.Values)
            {
                var filePath = appendToExecDir(Path.Combine(ISS_PATH, issFileName));
                var newFilePath = appendToExecDir(Path.Combine(ISS_PATH, Path.GetFileNameWithoutExtension(filePath) + ISS_GEN_SUFFIX + ".iss"));
#if DEBUG
                Console.WriteLine("Modifying: " + filePath + " -> " + newFilePath);
#endif

                if (!File.Exists(filePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FATAL: ISS file specified in mappings not found: " + issFileName + " -> " + filePath);
                    Console.ResetColor();
                    return false;
                }
                var fContent = File.ReadAllText(filePath);
                foreach (var key in variables.Keys)
                {
                    if (fContent.Contains(key))
                        fContent = fContent.Replace(key, variables[key]);
                }
                File.WriteAllText(newFilePath, fContent);
            }
            Console.WriteLine("All files have been written.");
            return true;
        }

        static bool DoChainCall(bool remove)
        {
            foreach (var key in fileMappings.Keys)
            {
                var setupFile = appendToExecDir(Path.Combine(SETUP_PATH, key));
                var issFile = "";
                if (Mode == OpMode.Install)
                    issFile = appendToExecDir(Path.Combine(ISS_PATH, Path.GetFileNameWithoutExtension(fileMappings[key]) + "_generated.iss"));
                else
                    issFile = appendToExecDir(Path.Combine(ISS_PATH, Path.GetFileNameWithoutExtension(fileMappings[key]) + "_uninstall.iss"));
                if (Mode == OpMode.Remove && !File.Exists(issFile)) //most likely an update which will be removed with the orig install file
                    continue;
                if (!File.Exists(setupFile))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("File specified in mappings does not exist: " + key + " -> " + setupFile);
                    Console.ResetColor();
                    return false;
                }
                var result = LaunchInstaller(setupFile, issFile, remove);
                if (result != 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("Setup " + key + "has exited with unexpected code! Expected: 0; Was: " + result);
                    Console.ResetColor();
                    Process.Start(appendToExecDir(LOG_FILE));
                    return false;
                }
            }
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("That's it! All files in chain have been executed!");
            Console.ResetColor();
            return true;
        }

        static int LaunchInstaller(string filePath, string issFile, bool remove)
        {
            Console.WriteLine("Running: " + Path.GetFileName(filePath));
            var p = new Process();
            if (remove)
            {
                p.StartInfo = new ProcessStartInfo(filePath, "/uninst /SMS /s /f2\"" + appendToExecDir(LOG_FILE) + "\" /f1\"" + issFile + "\"")
                {
                    UseShellExecute = false
                };
            }
            else
            {
                p.StartInfo = new ProcessStartInfo(filePath, "/s /SMS /f2\"" + appendToExecDir(LOG_FILE) + "\" /f1\"" + issFile + "\"")
                {
                    UseShellExecute = false
                };
            }

            try
            {
                p.Start();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Process Exec failed: " + ex.Message);
                Console.ResetColor();
                return 1;
            }

            p.WaitForExit();
            return p.ExitCode;
        }

        static void CleanUp()
        {
            Console.WriteLine("CleanUp triggered. Removing generated ISS files");
            DirectoryInfo issDir = new DirectoryInfo(appendToExecDir(ISS_PATH));
            var genFiles = issDir.GetFiles("*" + ISS_GEN_SUFFIX + ".iss");
            foreach (var file in genFiles)
                file.Delete();
            Console.WriteLine("All clean and shiny again.");
        }

        static string appendToExecDir(string dir)
        {
            if (WorkingDir == null || String.Empty == WorkingDir)
                return Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), dir);
            else
                return Path.Combine(WorkingDir, dir);
        }
    }

    enum OpMode
    {
        Install = 0,
        Remove = 1
    }
}
