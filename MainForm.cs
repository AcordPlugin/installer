using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AcordInstaller
{
    public partial class MainForm : Form
    {
        static string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public MainForm()
        {
            InitializeComponent();
            PrepareDestionations();
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            Install();
        }

        private void PrepareDestionations()
        {
            string[] possibleNames = new string[4] {"Discord", "DiscordPTB", "DiscordCanary", "DiscordDevelopment"};

            for (int i = 0; i < possibleNames.Length; i++)
            {
                string name = possibleNames[i];
                string path = Path.Combine(localAppData, name);
                if (Directory.Exists(path)) destinationComboBox.Items.Add(name);
            }
            destinationComboBox.Text = destinationComboBox.Items[0].ToString();
        }

        private void Install()
        {

            if (IsProcessOpen(destinationComboBox.Text))
            {
                MessageBox.Show($"{destinationComboBox.Text} is already running. Please quit {destinationComboBox.Text} first!", "Acord Installer");
                return;
            }

            installButton.Enabled = false;
            installButton.Text = "Installing..";

            Directory.CreateDirectory(Path.Combine(appData, "BetterDiscord/data"));
            Directory.CreateDirectory(Path.Combine(appData, "BetterDiscord/plugins"));

            string[] appPaths = Directory.GetDirectories(Path.Combine(localAppData, destinationComboBox.Text)).Where(i => Path.GetFileName(i).StartsWith("app-")).ToArray();

            string releasesResponse = HTTPGet("http://api.github.com/repos/BetterDiscord/BetterDiscord/releases/latest", true);
            Match match1 = Regex.Match(releasesResponse, @"(https:\/\/github\.com\/BetterDiscord\/BetterDiscord\/releases\/download\/[^/]+\/betterdiscord\.asar)");

            string betterAsarPath = Path.Combine(appData, "BetterDiscord/data/betterdiscord.asar");
            DownloadFile(match1.Groups[0].ToString(), betterAsarPath);

            DownloadFile("http://betterdiscord.app/Download?id=9", Path.Combine(appData, "BetterDiscord/plugins/0PluginLibrary.plugin.js"));
            DownloadFile("http://raw.githubusercontent.com/AcordPlugin/releases/main/acord.plugin.js", Path.Combine(appData, "BetterDiscord/plugins/acord.plugin.js"));

            for (int i = 0; i < appPaths.Length; i++)
            {
                string discordAppPath = appPaths[i];

                Directory.CreateDirectory(Path.Combine(discordAppPath, "resources/app"));
                File.WriteAllText(Path.Combine(discordAppPath, "resources/app/index.js"), $@"require(""{betterAsarPath.Replace("\\", "\\\\")}"");");
                File.WriteAllText(Path.Combine(discordAppPath, "resources/app/package.json"), "{\"name\":\"betterdiscord\",\"main\":\"index.js\"}");

                DownloadFile("https://github.com/GooseMod/OpenAsar/releases/download/nightly/app.asar", Path.Combine(discordAppPath, "resources/app.asar"));
            }

            installButton.Text = "Install";
            installButton.Enabled = true;

            MessageBox.Show($"Installation done for {destinationComboBox.Text}!", "Acord Installer");
        }

        public string HTTPGet(string uri, bool json = false)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.UserAgent = "Acord Installer";
            if (json) request.ContentType = "application/json";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public void DownloadFile(string uri, string path)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(uri, path);
            }
        }

        public bool IsProcessOpen(string name)
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                
                if (clsProcess.ProcessName == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
