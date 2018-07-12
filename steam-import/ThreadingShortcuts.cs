using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace steam_import
{
    public struct Record
    {
        public string AppName;
        public string Exe;
        public string StartDir;
        public string icon;
        public string ShortcutPath;
    }

    class ThreadingShortcuts
    {

        private Form1 parent;
        private string index_fp;
        private string games_vbam_fp;
        private string shortcuts_vdf_fp;
        private string output_dir_fp;

        public ThreadingShortcuts(Form1 parent, string index_fp, string games_vbam_fp, string shortcuts_vdf_fp, string output_fp)
        {
            this.parent = parent;
            this.index_fp = index_fp;
            this.games_vbam_fp = games_vbam_fp;
            this.shortcuts_vdf_fp = shortcuts_vdf_fp;
            this.output_dir_fp = output_fp;
        }

        public void StartAsync()
        {
            Directory.CreateDirectory(this.output_dir_fp + "\\config");

            Thread t = new Thread(T_Shortcuts);
            t.Start();

            return;

        }

        public void T_Shortcuts()
        {
            // loop import_list
            string file = File.ReadAllText(this.index_fp);
            string[] parts = file.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            int count = parts.Length;

            parent.Invoke(new Action(() => { parent.ProgressBar2Init(count + 1); }));

            List<string> records = GetRecords(this.shortcuts_vdf_fp);

            for (int i = 0; i < parts.Length; ++i)
            {
                string filename = parts[i].Substring(parts[i].IndexOf(";") + 1);
                string cat = Path.GetExtension(filename).Replace(".", "").ToUpper();
                filename = Path.GetFileNameWithoutExtension(filename);
                string index = parts[i].Substring(0, parts[i].IndexOf(";"));
                int ind = Int32.Parse(index);

                // create record
                Record r = new Record
                {
                    AppName = filename,
                    Exe = "\"" + this.games_vbam_fp + "\" rk " + ind.ToString(),
                    StartDir = "\"" + Path.GetDirectoryName(this.games_vbam_fp) + "\"",
                    icon = "",
                    ShortcutPath = ""
                };

                // add item to shortcuts.vdf
                records.Add(ShortcutsGetRecord(r, cat));
                parent.Invoke(new Action(() => { parent.ProgressBar2Increment(); }));
            }

            ShortcutsWriteRecords(records, this.output_dir_fp);
            parent.Invoke(new Action(() => { parent.ProgressBar2Increment(); }));
            return;
        }

        private static List<string> GetRecords(string f)
        {
            string file = File.ReadAllText(f);
            string intro = file.Substring(0, file.IndexOf("\x01"));

            string pattrn = @"\x01appname.*?\x08\x08";
            Regex rgx2 = new Regex(pattrn, RegexOptions.IgnoreCase);
            MatchCollection mtchs = rgx2.Matches(file);

            List<string> ret = new List<string>();

            ret.Add(intro);

            for (int i = 0; i < mtchs.Count; ++i)
            {
                ret.Add(Regex.Replace(mtchs[i].Value, @"\x02" + "lastplaytime.*tags", "\x02" + "LastPlayTime\x0\x0\x0\x0\x0\x0tags", RegexOptions.IgnoreCase));
            }


            return ret;

        }

        public static string ShortcutsGetRecord(Record r, string cat)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\x01" + "AppName\x0" + r.AppName + "\x0");
            sb.Append("\x01" + "Exe\x0" + r.Exe + "\x0");
            sb.Append("\x01" + "StartDir\x0" + r.StartDir + "\x0");
            sb.Append("\x01" + "icon\x0" + r.icon + "\x0");
            sb.Append("\x01" + "ShortcutPath\x0" + r.ShortcutPath + "\x0");

            sb.Append("\x01" + "LaunchOptions\x0\x0\x02" + "IsHidden\x0\x0\x0\x0\x0\x02" + "AllowDesktopConfig\x0\x01\x0\x0\x0\x02" + "AllowOverlay\x0\x01\x0\x0\x0\x02" + "OpenVR\x0\x0\x0\x0\x0\x02" + "LastPlayTime\x0\x0\x0\x0\x0\x0");
            sb.Append("tags\x0\x01" + "0\x0" + cat + "\x0\x08\x08");

            return sb.ToString();
        }

        public static void ShortcutsWriteRecords(List<string> records, string outpdir)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(records[0]);

            for (int i = 1; i < records.Count - 1; ++i)
            {
                sb.Append(records[i]);
                sb.Append("\x0" + i.ToString() + "\x0");

            }

            sb.Append(records[records.Count - 1]);
            sb.Append("\x08\x08");
            if (File.Exists(outpdir + "\\config\\shortcuts.vdf")) File.Delete(outpdir + "\\config\\shortcuts.vdf");
            File.WriteAllText(outpdir + "\\config\\shortcuts.vdf", sb.ToString());
        }
    }
}
