using System;
using System.Text;
using System.IO;

namespace ReportRunnerSupreme
{
    internal static class Log
    {
        public static string log_dir = @"C:\EncompassLog\";
        public static string log_filename = "log_ReportRunnerSupreme.txt";
        public static string log_path = "";
        private static readonly object lockObject = new object();

        public static bool prependWithTimestamp = true;

        public static void checkForPath(string fileName)
        {
            Log.log_path = String.Concat(log_dir, fileName);

            try
            {
                if (!Directory.Exists(log_dir))
                {
                    Directory.CreateDirectory(log_dir);
                    Console.WriteLine($"Directory created at {log_dir}");
                }
                else
                {
                    Console.WriteLine($"Directory already exists at {log_dir}");
                }

                if (!File.Exists(Log.log_path))
                {
                    File.Create(Log.log_path);
                    Console.WriteLine($"File created at {log_path}");
                }
                else
                {
                    Console.WriteLine($"File already exists at {log_path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating directory: {ex.Message}");
            }
        }
        public static void WriteLine(string msg, bool printToFile = true, bool printToConsole = true)
        {
            if (String.IsNullOrEmpty(msg.Trim())) { return; }

            StringBuilder sb = new StringBuilder();

            if (prependWithTimestamp)
            {
                sb.Append($"[{DateTime.Now.ToString()}] ");
            }

            sb.Append($"[DEBUG] {msg.Trim()} {Environment.NewLine}");

            if (printToConsole)
            {
                Console.Write(sb.ToString());
            }


            if (printToFile && !String.IsNullOrEmpty(log_path.Trim()))
            {
                WriteToFile(log_path, sb.ToString());
            }

        }

        public static void Write(string msg, bool printToFile = true, bool printToConsole = true)
        {
            if (String.IsNullOrEmpty(msg.Trim())) { return; }

            StringBuilder sb = new StringBuilder();

            if (prependWithTimestamp)
            {
                sb.Append($"[{DateTime.Now.ToString()}] ");
            }

            sb.Append($"[DEBUG] {msg.Trim()}");

            if (printToConsole)
            {
                Console.Write(sb.ToString());
            }


            if (printToFile && !String.IsNullOrEmpty(log_path.Trim()))
            {
                WriteToFile(log_path, sb.ToString());
            }

        }


        public static void WriteLineError(string msg, bool printToFile = true, bool printToConsole = true)
        {
            if (String.IsNullOrEmpty(msg.Trim())) { return; }

            StringBuilder sb = new StringBuilder();

            if (prependWithTimestamp)
            {
                sb.Append($"[{DateTime.Now.ToString()}] ");
            }

            sb.Append($"[ERROR] {msg.Trim()} {Environment.NewLine}");

            if (printToConsole)
            {
                Console.Write(sb.ToString());
            }


            if (!String.IsNullOrEmpty(log_path.Trim()))
            {
                WriteToFile(log_path, sb.ToString());
            }

        }

        public static void WriteError(string msg, bool printToFile = true, bool printToConsole = true)
        {
            if (String.IsNullOrEmpty(msg.Trim())) { return; }

            StringBuilder sb = new StringBuilder();

            if (prependWithTimestamp)
            {
                sb.Append($"[{DateTime.Now.ToString()}] ");
            }

            sb.Append($"[ERROR] {msg.Trim()}");

            if (printToConsole)
            {
                Console.Write(sb.ToString());
            }


            if (!String.IsNullOrEmpty(log_path.Trim()))
            {
                WriteToFile(log_path, sb.ToString());
            }

        }

        public static void WriteToFile(string sFile, string sText)
        {
            lock (lockObject)
            {
                try
                {
                    using (FileStream file = new FileStream(sFile, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(file))
                        {
                            sw.Write(sText);
                        }
                    }
                }
                catch
                {
                    {
                        try
                        {
                            File.AppendAllText(sFile, sText);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error writing file. ERROR: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
