using System.Drawing;
using System.Windows.Forms;

namespace games_vbam
{
    public partial class Loading : Form
    {
        public Loading()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        public void UpdateProgress(int value)
        {
            progressBar1.Value = value;
        }

    }
}
