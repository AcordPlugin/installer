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
using System.Threading;
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
            string[] possibleNames = new string[3] {"Discord", "DiscordPTB", "DiscordCanary"};

            for (int i = 0; i < possibleNames.Length; i++)
            {
                string name = possibleNames[i];
                string path = Path.Combine(localAppData, name);
                if (Directory.Exists(path)) destinationComboBox.Items.Add(name);
            }
            if (destinationComboBox.Items.Count > 0)
            {
                destinationComboBox.Text = destinationComboBox.Items[0].ToString();
            }
        }

        private void Install()
        {

            Process[] processes = Process.GetProcessesByName(destinationComboBox.Text).ToArray();

            string discordExePath = null;

            for (int i = 0; i < processes.Length; i++)
            {
                Process process = processes[i];
                
                try
                {
                    process.Kill();
                    if (discordExePath == null)
                    {
                        discordExePath = process.MainModule.FileName;
                        Thread.Sleep(100);
                    }
                } 
                catch 
                {

                }
                
            }

            installButton.Enabled = false;
            installButton.Text = "Installing..";
            TopMost = true;

            Directory.CreateDirectory(Path.Combine(appData, "BetterDiscord/data"));
            Directory.CreateDirectory(Path.Combine(appData, "BetterDiscord/plugins"));

            string bdReleaseType = "stable";

            switch (destinationComboBox.Text)
            {
                case "Discord": bdReleaseType = "stable"; break;
                case "DiscordPTB": bdReleaseType = "ptb"; break;
                case "DiscordCanary": bdReleaseType = "canary"; break;
            }

            string bdDataFolder = Path.Combine(appData, "BetterDiscord/data");

            string bdReleaseDataFolder = Path.Combine(bdDataFolder, bdReleaseType);

            Directory.CreateDirectory(bdReleaseDataFolder);

            File.WriteAllText(Path.Combine(bdReleaseDataFolder, "settings.json"), @"{""general"":{""publicServers"":false,""voiceDisconnect"":false,""showToasts"":true,""mediaKeys"":false},""addons"":{""addonErrors"":true,""editAction"":""detached""},""customcss"":{""customcss"":true,""liveUpdate"":false,""openAction"":""settings""},""editor"":{""lineNumbers"":true,""minimap"":true,""hover"":true,""quickSuggestions"":true,""fontSize"":14,""renderWhitespace"":""selection""},""window"":{""transparency"":false,""removeMinimumSize"":true,""frame"":false},""developer"":{""debugLogs"":false,""devTools"":true,""debuggerHotkey"":false,""reactDevTools"":true,""inspectElement"":false,""devToolsWarning"":false}}");
            File.WriteAllText(Path.Combine(bdReleaseDataFolder, "plugins.json"), @"{""ZeresPluginLibrary"":true,""Acord"":true,""BDFDB"":false}");

            string betterAsarPath = Path.Combine(bdDataFolder, "betterdiscord.asar");
            DownloadFile("https://github.com/AcordPlugin/releases/raw/main/betterdiscord.asar", betterAsarPath);

            string[] appPaths = Directory.GetDirectories(Path.Combine(localAppData, destinationComboBox.Text)).Where(i => Path.GetFileName(i).StartsWith("app-")).ToArray();

            File.WriteAllText(Path.Combine(appData, destinationComboBox.Text.ToLower(), "settings.json"), @"{""openasar"":{""setup"":true},""DANGEROUS_ENABLE_DEVTOOLS_ONLY_ENABLE_IF_YOU_KNOW_WHAT_YOURE_DOING"":true}");

            DownloadFile("http://betterdiscord.app/Download?id=9", Path.Combine(appData, "BetterDiscord/plugins/0PluginLibrary.plugin.js"));
            DownloadFile("http://raw.githubusercontent.com/AcordPlugin/releases/main/acord.plugin.js", Path.Combine(appData, "BetterDiscord/plugins/acord.plugin.js"));

            for (int i = 0; i < appPaths.Length; i++)
            {
                string discordAppPath = appPaths[i];

                string modulesPath = Path.Combine(discordAppPath, "modules");

                if (Directory.Exists(modulesPath))
                {
                    string[] desktopCoreModulePaths = Directory.GetDirectories(modulesPath).Where(k => Path.GetFileName(k).StartsWith("discord_desktop_core-")).ToArray();

                    for (int j = 0; j < desktopCoreModulePaths.Length; j++)
                    {
                        string modulePath = Path.Combine(desktopCoreModulePaths[j], "discord_desktop_core");

                        File.WriteAllText(Path.Combine(modulePath, "index.js"), $@"require(""{betterAsarPath.Replace("\\", "/")}"");module.exports = require(""./core.asar"");");
                        File.WriteAllText(Path.Combine(modulePath, "package.json"), "{\"name\":\"betterdiscord\",\"main\":\"index.js\",\"version\":\"0.0.0\"}");
                    }

                    Directory.CreateDirectory(Path.Combine(discordAppPath, "resources/app"));
                    DownloadFile("https://github.com/GooseMod/OpenAsar/releases/download/nightly/app.asar", Path.Combine(discordAppPath, "resources/app.asar"));
                }
                
            }

            if (discordExePath != null)
            {
                Thread.Sleep(100);
                Process.Start(discordExePath);
            }

            installButton.Text = "Install";
            installButton.Enabled = true;

            DialogResult resp = MessageBox.Show($"Installation done for {destinationComboBox.Text}! Do you want to exit installer?", "Acord Installer", MessageBoxButtons.YesNo);
            if (resp == DialogResult.Yes) this.Close();
            TopMost = false;
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (destinationComboBox.Items.Count == 0)
            {
                MessageBox.Show("There is no Discord installation found on this computer.", "Acord Installer");
                this.Close();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://acord.app/");
        }
    }
}
