using Dokan;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MergeFS
{
    public partial class Form1 : Form, Logger
    {

        MergedFileSystem mfs;

        public Form1()
        {
            InitializeComponent();

            populateAvaiableDriveLetters();
        }

        public void mount(string driveLetter)
        {

            List<Root> roots = new List<Root>();
            foreach (string item in listBox1.Items)
            {
                roots.Add(new Root(item));
            }
            mfs = new MergedFileSystem(this,roots);

            DokanOptions opt = new DokanOptions();
            opt.DebugMode = false;
          
            opt.MountPoint = driveLetter+":\\";
            opt.ThreadCount = 5;
            opt.VolumeLabel = "MergeFS";
            opt.RemovableDrive = false;
            opt.NetworkDrive = false;

            int status = DokanNet.DokanMain(opt, mfs);
            switch (status)
            {
                case DokanNet.DOKAN_DRIVE_LETTER_ERROR:
                    Console.WriteLine("Drvie letter error");
                    break;
                case DokanNet.DOKAN_DRIVER_INSTALL_ERROR:
                    Console.WriteLine("Driver install error");
                    break;
                case DokanNet.DOKAN_MOUNT_ERROR:
                    Console.WriteLine("Mount error");
                    break;
                case DokanNet.DOKAN_START_ERROR:
                    Console.WriteLine("Start error");
                    break;
                case DokanNet.DOKAN_ERROR:
                    Console.WriteLine("Unknown error");
                    break;
                case DokanNet.DOKAN_SUCCESS:
                    Console.WriteLine("Success");
                    break;
                default:
                    Console.WriteLine("Unknown status: %d", status);
                    break;

            }
        }

    

        public void addLog(string message)
        {
            this.Invoke( (Action) delegate{ addLogMSG(message); });
        }

        public void addLogMSG(string message)
        {
            System.Console.WriteLine(message);
            logBox.Text = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now) + ": " + message + "\r\n" + logBox.Text;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Invoke((Action)delegate { AvailableDrives.Enabled = false; refreshAvailableDrivesButton.Enabled = false; mountButton.Enabled = false; unmountButton.Enabled = true; addPointButton.Enabled = false; });
            mount((string)""+(char)e.Argument);
            this.Invoke((Action)delegate { AvailableDrives.Enabled = true; refreshAvailableDrivesButton.Enabled = true; mountButton.Enabled = true; unmountButton.Enabled = false; addPointButton.Enabled = true; });
        }

        private void mountButton_Click(object sender, EventArgs e)
        {
            addLog("Starting mount");
            backgroundWorker1.RunWorkerAsync(AvailableDrives.SelectedItem);
        }

        private void unmountButton_Click(object sender, EventArgs e)
        {
            unmount();
        }

        public void unmount()
        {
            addLog("Starting unmount");
            DokanNet.DokanUnmount((char)AvailableDrives.SelectedItem);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            unmount();
        }

        private void addPointButton_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == folderBrowserDialog1.ShowDialog(this))
            {
                listBox1.Items.Add(folderBrowserDialog1.SelectedPath);
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0 && mountButton.Enabled)
            {
                removePointButton.Enabled = true;
            }
            else
            {
                removePointButton.Enabled = false;
            }
        }

        private void populateAvaiableDriveLetters()
        {
            List<char> driveLetters = new List<char>(26); // Allocate space for alphabet
            for (int i = 69; i < 91; i++) // increment from ASCII values for A-Z
            {
                driveLetters.Add(Convert.ToChar(i)); // Add uppercase letters to possible drive letters
            }

            foreach (string drive in Directory.GetLogicalDrives())
            {
                driveLetters.Remove(drive[0]); // removed used drive letters from possible drive letters
            }
            AvailableDrives.Items.Clear();

            foreach (char drive in driveLetters)
            {
                AvailableDrives.Items.Add(drive); // add unused drive letters to the combo box
            }
            AvailableDrives.SelectedIndex = 0;
        }

        private void refreshAvailableDrivesButton_Click(object sender, EventArgs e)
        {
            populateAvaiableDriveLetters();
        }

        private void AvailableDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void removePointButton_Click(object sender, EventArgs e)
        {
            listBox1.Items.Remove(listBox1.SelectedItem);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

    }
}
