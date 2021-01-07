#nullable enable

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
                // FIXXME: inform about old shots in the folder
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
                    // FIXXME: method to convert old, existing bitmaps in the screenshots folder
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
        
        private static string getTargetFileName(bool highres = false)
        {
            StringBuilder sb = new StringBuilder(VA!.GetText("EliteScreenshots.format#") ?? defaultFormat);
            MatchCollection matches = tokenRegex.Matches(sb.ToString());

            string token;
            string value;
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
                    newFileName = Path.Combine(outputDirectory, $"{sb}_{i:D4}.png");
                    i++;
                } while (File.Exists(newFileName));

                targetFilename = newFileName;
            }

            return targetFilename;
        }

        private static string ConvertAndMove(string file, bool highres = false)
        {
            string target = getTargetFileName(highres);

            using (Bitmap bm = new Bitmap(file)) { 
                bm.Save(target, ImageFormat.Png);
            }
            File.Delete(file);

            return target;
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
                    LogInfo($"New screenshot found, moving to '{ConvertAndMove(Path.Combine(screenshotsDirectory, name))}'");
                }
                else if (highResRegex.IsMatch(name))
                {
                    
                    LogInfo($"New high resolution screenshot found, moving to '{ConvertAndMove(Path.Combine(screenshotsDirectory, name))}'");
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
