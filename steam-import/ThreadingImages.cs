using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace steam_import
{
    public struct TData
    {
        public string part;
        public int wait;
    }

    public class ThreadingImages
    {

        private string index_file;
        private string outp_dir;
        private string games_vbam_path;
        private Form1 parent;

        public int maxThreads = 32;

        public Dictionary<string, string> urls = new Dictionary<string, string>()
        {
            { "GB", "https://raw.githubusercontent.com/libretro/libretro-thumbnails/master/Nintendo%20-%20Game%20Boy/Named_Boxarts/{0}.png" },
            { "GBC", "https://raw.githubusercontent.com/libretro/libretro-thumbnails/master/Nintendo%20-%20Game%20Boy%20Color/Named_Boxarts/{0}.png" },
            { "GBA", "https://raw.githubusercontent.com/libretro/libretro-thumbnails/master/Nintendo%20-%20Game%20Boy%20Advance/Named_Boxarts/{0}.png"}
        };
        public Stack<string> parts = new Stack<string>();


        private Object _parts_lock = new Object();
        private Object _incrementer_lock = new Object();

        public ThreadingImages(Form1 parent, string index_file, string outp_dir, string games_vbam_path)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = maxThreads;
            this.parent = parent;
            this.index_file = index_file;
            this.outp_dir = outp_dir;
            this.games_vbam_path = games_vbam_path;
        }


        public void StartAsync()
        {
            WebRequest.DefaultWebProxy = null;

            string file = File.ReadAllText(index_file);
            string[] p = file.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
            //int cnt = 0;
            foreach (string pp in p)
            {
                //if (cnt == 25) break;
                parts.Push(pp);
                //cnt++;
            }

            int count = parts.Count;
            
            parent.Invoke(new Action(() => { parent.ProgressBar1Init(count); }));

            //Thread t = new Thread(T_Dispatcher);
            //t.Start();

            Directory.CreateDirectory(outp_dir + "\\config\\grid");

            for(int i = 0; i < maxThreads; ++i)
            {
                Thread t = new Thread(T_Downloader);
                lock (_parts_lock)
                {
                    try
                    {
                        t.Start(new TData { part = parts.Pop(),  wait = i * 250 });
                    }
                    catch { break; }
                }
            }

            return;

        }

        //private void T_Dispatcher()
        //{

        //}

        private void T_Downloader(object t_data)
        {
            TData tdata = (TData)t_data;

            //if (tdata.sleep)
            //{
            //Random rnd = new Random();
            //int n = rnd.Next(0, this.maxThreads * 500);
            Thread.Sleep(tdata.wait);
            //}

            string part = (string)tdata.part;

            //lock (_parts_lock)
            //{
            //    parts.Remove(part);
            //}

            string filename = part.Split(new string[] { ";" }, StringSplitOptions.None)[1];
            string id = part.Substring(0, part.IndexOf(";"));
            int ind = Int32.Parse(id);

            string target = "\"" + games_vbam_path + "\" rk " + id;
            string title = Path.GetFileNameWithoutExtension(filename);

            string app_id = GA.GetApplicationId(target + title);

            string cat = Path.GetExtension(filename).Replace(".", "").ToUpper();
            filename = Path.GetFileNameWithoutExtension(filename);
            filename = filename.Replace("&", "_");

            string url = String.Format(urls[cat], filename);



            try
            {

                WebClient wc = new WebClient();
                //wc.Proxy = null;
                byte[] img = wc.DownloadData(url);

                Bitmap ret = (Bitmap)Bitmap.FromStream(new MemoryStream(img));
                ret = GP.Convert(ret, cat);
                ret.Save(outp_dir + "\\config\\grid\\" + app_id + ".png", ImageFormat.Png);

            }
            catch
            {
                //parent.Invoke(new Action(() => { parent.Log("img for " + filename + " not found!"); }));

            }
            parent.Invoke(new Action(() => { parent.ProgressBar1Increment(); }));


            lock (_parts_lock)
            {
                try
                {
                    Thread t = new Thread(T_Downloader);
                    t.Start(new TData { part = parts.Pop(), wait = 0 });
                }
                catch {; }
            }






            return;
        }

    }
}
