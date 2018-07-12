using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace steam_import
{
    public partial class Form1 : Form
    {
        public bool ImagesFinished = false;
        public bool ShortcutsFinished = false;
        public Stopwatch stopWatch = new Stopwatch();

        public Form1()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            textBox2.Text = Directory.GetCurrentDirectory() + "\\INDEX_USA_GBA.TXT";
            textBox2.Select(textBox2.TextLength, 0);
            textBox4.Text = Directory.GetCurrentDirectory() + "\\games-vbam\\games-vbam.exe";
            textBox4.Select(textBox4.TextLength, 0);
            textBox3.Text = Directory.GetCurrentDirectory() + "\\OUTPUT";
            textBox3.Select(textBox3.TextLength, 0);


        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!File.Exists(textBox2.Text))
            {
                Log("Error : INDEX.TXT file not found!");
                return;
            }

            if (!File.Exists(textBox4.Text))
            {
                Log("Error : games-vbam.exe file not found!");
                return;
            }

            if (!File.Exists(textBox1.Text))
            {
                Log("Error : shortcuts.vdf file not found!");
                return;
            }

            Log("Running...");
            stopWatch.Start();

            ThreadingImages ti = new ThreadingImages(this,textBox2.Text, textBox3.Text, textBox4.Text);
            ti.StartAsync();

            ThreadingShortcuts ts = new ThreadingShortcuts(this, textBox2.Text, textBox4.Text, textBox1.Text, textBox3.Text);
            ts.StartAsync();

            panel1.Enabled = false;

            return;
        }

        public void Continue(int control)
        {
            if (control == 0) ImagesFinished = true;
            if (control == 1) ShortcutsFinished = true;

            if (ImagesFinished && ShortcutsFinished)
            {
                Log("Completed!");
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
                Log("RunTime " + elapsedTime);
            }

            return;
        }

        public void Log(string message)
        {
            listBox1.Items.Add(message);
            listBox1.TopIndex = listBox1.Items.Count - 1;
            listBox1.Refresh();
            return;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            DialogResult res = openFileDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
                textBox2.Select(textBox2.TextLength, 0);
            }

            return;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            DialogResult res = openFileDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBox4.Text = openFileDialog1.FileName;
                textBox4.Select(textBox4.TextLength, 0);
            }

            return;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Directory.GetCurrentDirectory();
            DialogResult res = openFileDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                textBox1.Select(textBox1.TextLength, 0);
            }

            return;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = Directory.GetCurrentDirectory();
            DialogResult res = folderBrowserDialog1.ShowDialog();
            if (res == DialogResult.OK)
            {
                textBox3.Text = folderBrowserDialog1.SelectedPath;
                textBox3.Select(textBox3.TextLength, 0);
            }
        }

        public void ProgressBar1Init(int cnt)
        {
            progressBar1.Maximum = cnt;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            return;
        }

        public void ProgressBar1Increment()
        {
            progressBar1.Increment(1);
            if (progressBar1.Value == progressBar1.Maximum)
                Continue(0);
            return;
        }

        public void ProgressBar2Init(int cnt)
        {
            progressBar2.Maximum = cnt;
            progressBar2.Step = 1;
            progressBar2.Value = 0;
            return;
        }

        public void ProgressBar2Increment()
        {
            progressBar2.Increment(1);
            if (progressBar2.Value == progressBar2.Maximum)
                Continue(1);
            return;
        }
    }
}
