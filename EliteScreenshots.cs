// <copyright file="EliteScreenshots.cs" company="alterNERDtive">
// Copyright 2021–2022 alterNERDtive.
//
// This file is part of VoiceAttack EliteScreenshots plugin.
//
// VoiceAttack EliteScreenshots plugin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// VoiceAttack EliteScreenshots plugin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with VoiceAttack EliteScreenshots plugin.  If not, see &lt;https://www.gnu.org/licenses/&gt;.
// </copyright>

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
    /// <summary>
    /// VoiceAttack plugin that automatically detects, converts and moves
    /// screenshots created by Elite Dangerous in the background.
    /// </summary>
    public class EliteScreenshots
    {
        private static readonly string ScreenshotsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), @"Frontier Developments\Elite Dangerous");
        private static readonly string DefaultFormat = "%datetime%-%cmdr%-%system%-%body%";
        private static readonly string DefaultOutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        private static readonly Regex StandardRegex = new (@"^Screenshot_\d{4}\.bmp$");
        private static readonly Regex HighResRegex = new (@"^HighResScreenShot_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}.bmp$");
        private static readonly Regex TokenRegex = new (@"%(?<token>[\w: \-\.]*)%");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "just cause")]
        private static dynamic? VA;
        private static FileSystemWatcher? fileWatcher;

        private static FileSystemWatcher FileWatcher
        {
            get
            {
                if (fileWatcher == null)
                {
                    fileWatcher = new FileSystemWatcher(ScreenshotsDirectory);
                    fileWatcher.Created += (source, eventArgs) => { FileChangedHandler(eventArgs); };
                }

                return fileWatcher!;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "nicer grouping")]
        private static readonly Version VERSION = new ("0.1");

        /// <summary>
        /// The plugin’s display name, as required by the VoiceAttack plugin API.
        /// </summary>
        /// <returns>The display name.</returns>
        public static string VA_DisplayName() => $"EliteScreenshots Plugin {VERSION}";

        /// <summary>
        /// The plugin’s description, as required by the VoiceAttack plugin API.
        /// </summary>
        /// <returns>The description.</returns>
        public static string VA_DisplayInfo() => VA_DisplayName();

        /// <summary>
        /// The plugin’s GUID, as required by the VoiceAtatck plugin API.
        /// </summary>
        /// <returns>The GUID.</returns>
        public static Guid VA_Id() => new Guid("{252490FD-2E6F-4703-900B-02ED98D717C2}");

        /// <summary>
        /// The Init method, as required by the VoiceAttack plugin API.
        /// Runs when the plugin is initially loaded.
        /// </summary>
        /// <param name="vaProxy">The VoiceAttack proxy object.</param>
        public static void VA_Init1(dynamic vaProxy)
        {
            VA = vaProxy;
            VA.TextVariableChanged += new Action<string, string, string, Guid?>(TextVariableChanged);

            VA.SetText("EliteScreenshots.version", VERSION);

            try
            {
                // inform about old shots
                DirectoryInfo dirInfo = new DirectoryInfo(ScreenshotsDirectory);
                int standardCount = dirInfo.GetFiles().Where(file => StandardRegex.IsMatch(file.Name)).Count();
                int highResCount = dirInfo.GetFiles().Where(file => HighResRegex.IsMatch(file.Name)).Count();

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

        /// <summary>
        /// The Invoke method, as required by the VoiceAttack plugin API.
        /// Runs whenever a plugin context is invoked.
        /// </summary>
        /// <param name="vaProxy">The VoiceAttack proxy object.</param>
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

        /// <summary>
        /// The Exit method, as required by the VoiceAttack plugin API.
        /// Runs when VoiceAttack is shut down.
        /// </summary>
        /// <param name="vaProxy">The VoiceAttack proxy object.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "required by VoiceAttack plugin API")]
        public static void VA_Exit1(dynamic vaProxy)
        {
        }

        /// <summary>
        /// The StopCommand method, as required by the VoiceAttack plugin API.
        /// Runs whenever all commands are stopped using the “Stop All Commands”
        /// button or action.
        /// </summary>
        public static void VA_StopCommand()
        {
        }

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

        private static string GetTargetFileName(bool highres = false, string? fromFile = null)
        {
            StringBuilder sb = new StringBuilder(VA!.GetText("EliteScreenshots.format#") ?? DefaultFormat);
            MatchCollection matches = TokenRegex.Matches(sb.ToString());

            string token;
            string value;
            if (string.IsNullOrEmpty(fromFile))
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

            string outputDirectory = VA!.GetText("EliteScreenshots.outputDirectory#") ?? DefaultOutputDirectory;
            string targetFilename = Path.Combine(outputDirectory, $"{sb}{(highres ? "-highres" : string.Empty)}.png");

            if (File.Exists(targetFilename))
            {
                int i = 1;

                string newFileName;
                do
                {
                    newFileName = Path.Combine(outputDirectory, $"{sb}{(highres ? "-highres" : string.Empty)}_{i:D4}.png");
                    i++;
                }
                while (File.Exists(newFileName));

                targetFilename = newFileName;
            }

            return targetFilename;
        }

        private static string ConvertAndMove(string source, string? target = null, bool highres = false)
        {
            target ??= GetTargetFileName(highres);

            using (Bitmap bm = new Bitmap(source))
            {
                bm.Save(target, ImageFormat.Png);
            }

            LogInfo($"Saved{(highres ? " high resolution" : string.Empty)} screenshot to '{target}'.");

            File.Delete(source);

            return target;
        }

        private static void ConvertOldShots()
        {
            DirectoryInfo dirInfo = new (ScreenshotsDirectory);

            foreach (FileInfo fileInfo in dirInfo.GetFiles().Where(file => StandardRegex.IsMatch(file.Name)).ToList())
            {
                ConvertAndMove(fileInfo.FullName, target: GetTargetFileName(fromFile: fileInfo.FullName));
            }

            foreach (FileInfo fileInfo in dirInfo.GetFiles().Where(file => HighResRegex.IsMatch(file.Name)).ToList())
            {
                ConvertAndMove(fileInfo.FullName, target: GetTargetFileName(fromFile: fileInfo.FullName, highres: true), highres: true);
            }
        }

        private static void TextVariableChanged(string name, string from, string to, Guid? internalID = null)
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
                if (StandardRegex.IsMatch(name))
                {
                    ConvertAndMove(Path.Combine(ScreenshotsDirectory, name));
                }
                else if (HighResRegex.IsMatch(name))
                {
                    // This is ugly AF …
                    // But I have to wait for Elite to finish writing, and no,
                    // I have not been able to find a viable alternative to this
                    // that would be less ugly.
                    Thread.Sleep(5000);
                    ConvertAndMove(Path.Combine(ScreenshotsDirectory, name), highres: true);
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
