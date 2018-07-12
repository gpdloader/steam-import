using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace games_vbam
{
    static class Program
    {
        static void ProcessStart(string rom_name)
        {
            string file = File.ReadAllText("path.ini");
            file = file.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0];
            Process p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C " + String.Format(file, "\"" + Directory.GetCurrentDirectory() + "\\cache\\" + rom_name + "\"");
            p.Start();
            return;
        }

        public static Loading l;
        public static byte[] game = null;

        [STAThread]
        static int Main(string[] args)
        {
            // PARSE ARGS
            if (args.Length != 2) return -1;
            if (args[0] != "rk") return -1;          //unused
            string index = args[1];
            Int32 ind = Int32.Parse(index);

            Directory.CreateDirectory("cache");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            l = new Loading();

            //CHECK IF FILE EXISTS
            string[] files = Directory.GetFiles("cache", ind.ToString() + ".gb*");
            if (files.Length != 0)
            {
                //l.Show();
                ProcessStart(Path.GetFileName(files[0]));
                return 0;
            }

            //DOWNLOAD FILE
            Thread thread = new Thread(() => {
                WebRequest.DefaultWebProxy = null;
                WebClient client = new WebClient();
                client.Proxy = null;
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadDataCompleted += Client_DownloadFileCompleted;
                client.DownloadDataAsync(new Uri("http://edgeemu.net/download.php?id=" + ind.ToString()));
            });
            thread.Start();

            Application.Run(l);

            if (game == null) { return -1; }

            //EXTRACT FILE
            List<string> validext = new List<string>() { ".gb", ".gbc", ".gba" };
            File.WriteAllBytes("tmp.bin", game);
            ZipArchive za = ZipFile.Open("tmp.bin", ZipArchiveMode.Read);
            string ext = Path.GetExtension(za.Entries[0].Name);
            if (!validext.Contains(ext))
                return -1;

            string rom_name = ind.ToString() + ext;
            za.Entries[0].ExtractToFile("cache\\" + rom_name);
            za.Dispose();
            File.Delete("tmp.bin");
            ProcessStart(rom_name);
            return 0;
        }

        private static void Client_DownloadFileCompleted(object sender, DownloadDataCompletedEventArgs eventArgs)
        {
            try
            {
                game = eventArgs.Result;
                l.Close();
            }
            catch {; }
            return;
        }

        private static void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                l.UpdateProgress(int.Parse(Math.Truncate(percentage).ToString()));
            }
            catch { ; }
            return;
        }
    }
}
