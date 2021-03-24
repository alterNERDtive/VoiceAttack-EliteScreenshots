#nullable enable

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace EliteScreenshots
{
    public class EliteScreenshots
    {
        private static dynamic? VA;
        private static readonly string screenshotsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), @"Frontier Developments\Elite Dangerous");
        private static readonly string defaultFormat = "%datetime%-%cmdr%-%system%-%body%";
        private static readonly string defaultOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        private static readonly Regex standardRegex = new Regex(@"^Screenshot_\d{4}\.bmp$");
        private static readonly Regex highResRegex = new Regex(@"^HighResScreenShot_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}.bmp$");
        private static readonly Regex tokenRegex = new Regex(@"%(?<token>[\w: \-\.]*)%");

        private static FileSystemWatcher FileWatcher
        {
            get
            {
                if (fileWatcher == null)
                {
                    fileWatcher = new FileSystemWatcher(screenshotsDirectory);
                    fileWatcher.Created += (source, EventArgs) => { FileChangedHandler(EventArgs); };
                }
                return fileWatcher!;
            }
        }
        private static FileSystemWatcher? fileWatcher;

        public static string VERSION = "0.1";

        public static string VA_DisplayName() => $"EliteScreenshots Plugin {VERSION}";

        public static string VA_DisplayInfo() => VA_DisplayName();

        public static Guid VA_Id() => new Guid("{252490FD-2E6F-4703-900B-02ED98D717C2}");

        public static void VA_Init1(dynamic vaProxy)
        {
            VA = vaProxy;
            VA.TextVariableChanged += new Action<string, string, string, Guid?>(TextVariableChanged);

            VA.SetText("EliteScreenshots.version", VERSION);

            try
            {
                // inform about old shots

                DirectoryInfo dirInfo = new DirectoryInfo(screenshotsDirectory);
                int standardCount = dirInfo.GetFiles().Where(file => standardRegex.IsMatch(file.Name)).Count();
                int highResCount = dirInfo.GetFiles().Where(file => highResRegex.IsMatch(file.Name)).Count();

                if (standardCount > 0 && highResCount > 0)
                {
                    LogInfo($"There are {standardCount} old screenshots and {highResCount} old high res screenshots.");
                }
                else if (standardCount > 0)
                {
                    LogInfo($"There are {standardCount} old screenshots.");
                }
                else if (highResCount > 0)
                {
                    LogInfo($"There are {highResCount} old high res screenshots.");
                }

                if (standardCount > 0 || highResCount > 0)
                {
                    LogInfo($"Run the “convertold” plugin context to convert them.");
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
            finally
            {
                FileWatcher.EnableRaisingEvents = true;
            }
        }

        public static void VA_Invoke1(dynamic vaProxy)
        {
            VA = vaProxy;
            try
            {
                string context = VA.Context.ToLower();
                if (context == "convertold")
                {
                    ConvertOldShots();
                }
                else
                {
                    LogError($"Invalid plugin context: '{context}'.");
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
        }

        public static void VA_StopCommand() { }

        public static void VA_Exit1(dynamic vaProxy) { }

        private static void LogError(string message)
        {
            VA!.WriteToLog($"ERROR | EliteScreenshots: {message}", "red");
        }

        private static void LogInfo(string message)
        {
            VA!.WriteToLog($"INFO | EliteScreenshots: {message}", "blue");
        }

        private static void LogWarn(string message)
        {
            VA!.WriteToLog($"WARN | EliteScreenshots: {message}", "yellow");
        }
        
        private static string getTargetFileName(bool highres = false, string? fromFile = null)
        {
            StringBuilder sb = new StringBuilder(VA!.GetText("EliteScreenshots.format#") ?? defaultFormat);
            MatchCollection matches = tokenRegex.Matches(sb.ToString());

            string token;
            string value;
            if (String.IsNullOrEmpty(fromFile))
            {
                foreach (Match match in matches)
                {
                    token = match.Groups["token"].Value;

                    value = token switch
                    {
                        "body" => VA!.GetText("Status body name") ?? "unknown",
                        "cmdr" => VA!.GetText("Name") ?? "unknown",
                        "date" => DateTime.Now.ToString("yyyy-MM-dd"),
                        "datetime" => DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"),
                        "shipname" => VA!.GetText("Ship name") ?? "unknown",
                        "system" => VA!.GetText("System name") ?? "unknown",
                        "time" => DateTime.Now.ToString("HH-mm-ss"),
                        "vehicle" => VA!.GetText("Status vehicle") ?? "unknown",
                        _ => $"%{token}%",
                    };
                    sb.Replace($"%{token}%", value);
                }
            }
            else
            {
                foreach (Match match in matches)
                {
                    token = match.Groups["token"].Value;

                    DateTime fileDateTime = File.GetCreationTime(fromFile);
                    value = token switch
                    {
                        "body" => "unknown",
                        "cmdr" => "unknown",
                        "date" => fileDateTime.ToString("yyyy-MM-dd"),
                        "datetime" => fileDateTime.ToString("yyyy-MM-dd HH-mm-ss"),
                        "shipname" => "unknown",
                        "system" => "unknown",
                        "time" => fileDateTime.ToString("HH-mm-ss"),
                        "vehicle" => "unknown",
                        _ => $"%{token}%",
                    };
                    sb.Replace($"%{token}%", value);
                }
            }

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                sb.Replace(c, '_');
            }

            string outputDirectory = VA!.GetText("EliteScreenshots.outputDirectory#") ?? defaultOutputDirectory;
            string targetFilename = Path.Combine(outputDirectory, $"{sb}{(highres ? "-highres" : "")}.png");

            if (File.Exists(targetFilename))
            {
                int i = 1;

                string newFileName;
                do
                {
                    newFileName = Path.Combine(outputDirectory, $"{sb}{(highres ? "-highres" : "")}_{i:D4}.png");
                    i++;
                } while (File.Exists(newFileName));

                targetFilename = newFileName;
            }

            return targetFilename;
        }

        private static string ConvertAndMove(string source, string? target = null, bool highres = false)
        {
            target ??= getTargetFileName(highres);

            using (Bitmap bm = new Bitmap(source)) { 
                bm.Save(target, ImageFormat.Png);
            }

            LogInfo($"Saved{(highres ? " high resolution" : "")} screenshot to '{target}'.");

            File.Delete(source);

            return target;
        }

        private static void ConvertOldShots ()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(screenshotsDirectory);
            foreach (FileInfo fileInfo in dirInfo.GetFiles().Where(file => standardRegex.IsMatch(file.Name)).ToList())
            {
                ConvertAndMove(fileInfo.FullName, target: getTargetFileName(fromFile: fileInfo.FullName));
            }
            foreach (FileInfo fileInfo in dirInfo.GetFiles().Where(file => highResRegex.IsMatch(file.Name)).ToList())
            {
                ConvertAndMove(fileInfo.FullName, target: getTargetFileName(fromFile: fileInfo.FullName, highres: true), highres: true);
            }
        }

        public static void TextVariableChanged(string name, string from, string to, Guid? internalID)
        {
            if (name == "EliteScreenshots.format#")
            {
                // FIXXME: check if actually valid, error otherwise
                // (or just give example output along with it?)
                LogInfo($"Output format changed to '{to}'.");
            }
            if (name == "EliteScreenshots.outputDirectory#")
            {
                // FIXXME check if it exists
                LogInfo($"Output directory changed to '{to}'.");
            }
        }

        private static void FileChangedHandler(FileSystemEventArgs eventArgs)
        {
            string name = eventArgs.Name;
            try
            {
                if (standardRegex.IsMatch(name))
                {
                    ConvertAndMove(Path.Combine(screenshotsDirectory, name));
                }
                else if (highResRegex.IsMatch(name))
                {
                    // This is ugly AF …
                    // But I have to wait for Elite to finish writing, and no,
                    // I have not been able to find a viable alternative to this
                    // that would be less ugly.
                    Thread.Sleep(5000);
                    ConvertAndMove(Path.Combine(screenshotsDirectory, name), highres: true);
                }
                else
                {
                    throw new Exception($"Found new file '{name}', but it does not appear to be a screenshot.");
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
            }
        }
    }
}
