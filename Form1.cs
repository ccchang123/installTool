using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace installTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkedListBox1.SetItemChecked(0, true);
            checkedListBox1.SetItemChecked(1, true);
            checkedListBox1.SetItemChecked(2, true);

            var yamlReader = new YamlReader();
            InstallerConfig config = yamlReader.ReadYamlFile("installers.yml");
            if (string.IsNullOrEmpty(config.DownloadURL))
            {
                addMessageToListBox("�]�w�ɿ��~�GdownloadURL �Ȭ���");
                checkedListBox1.Enabled = false;
                return;
            }

            foreach (var installer in config.Installers)
            {
                if (string.IsNullOrEmpty(installer.Name))
                {
                    addMessageToListBox("�]�w�ɿ��~�Ginstallers.name �Ȭ���");
                    checkedListBox1.Enabled = false;
                    return;
                }
                if (string.IsNullOrEmpty(installer.File))
                {
                    addMessageToListBox("�]�w�ɿ��~�Ginstallers.file �Ȭ���");
                    checkedListBox1.Enabled = false;
                    return;
                }
                if (installer.SetDefaultEnable == null)
                {
                    addMessageToListBox("�]�w�ɿ��~�Ginstallers.setDefaultEnable �Ȭ���");
                    checkedListBox1.Enabled = false;
                    return;
                }

                int index = checkedListBox1.Items.Add(installer.Name);
                if (installer.SetDefaultEnable)
                {
                    checkedListBox1.SetItemChecked(index, true);
                }
            }
            if (!IsProgramInstalled("Chrome"))
            {
                checkedListBox1.SetItemChecked(3, true);
                addMessageToListBox("�����쥼�w��Chrome�A�w�۰ʤĿ�ӿﶵ�C");
            }
        }

        private void checkedListBox1_MouseClick(object sender, MouseEventArgs e)
        {
            int index = checkedListBox1.IndexFromPoint(e.Location);

            if (index != ListBox.NoMatches)
            {
                bool isChecked = checkedListBox1.GetItemChecked(index);
                checkedListBox1.SetItemChecked(index, !isChecked);
                checkedListBox1.SetSelected(index, false);

                if (checkedListBox1.Items[index].ToString() == "�ҥ�Windows" && !isChecked)
                {
                    checkedListBox1.SetItemChecked(1, true);
                    checkedListBox1.SetItemChecked(2, true);
                }
                else if (checkedListBox1.Items[index].ToString() == "�ҥ�Windows" && isChecked)
                {
                    checkedListBox1.SetItemChecked(1, false);
                    checkedListBox1.SetItemChecked(2, false);
                }
            }

            if (checkedListBox1.CheckedItems.Count == 0)
            {
                button1.Enabled = false;
            }
            else
            {
                button1.Enabled = true;
            }
        }

        private void ExecuteCmd(string command)
        {
            try
            {
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c " + command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    }
                };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                addMessageToListBox("���楢�ѡG" + e.Message);
            }
        }

        async private void button1_Click(object sender, EventArgs e)
        {
            checkedListBox1.Enabled = false;
            button1.Enabled = false;

            List<Task> tasks = new List<Task>();
            string folderPath = @"temp";
            var yamlReader = new YamlReader();
            InstallerConfig config = yamlReader.ReadYamlFile("installers.yml");
            string baseURL = config.DownloadURL;

            if (!Directory.Exists(folderPath))
            {
                addMessageToListBox("�ͦ��Ȧs��...");
                Directory.CreateDirectory(folderPath);
            }
            if (checkedListBox1.CheckedItems.Contains("�ҥ�Windows"))
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", writable: true))
                        {
                            if (key == null) 
                            { 
                                addMessageToListBox("����ɵo�Ϳ��~�G�L�k�s�����U����");
                                return;
                            }
                            addMessageToListBox("���b�����U��GKMS���A��...");
                            key.SetValue("KeyManagementServiceName", "skms.netnr.eu.org", RegistryValueKind.String);
                            addMessageToListBox("���b�����U��GKMS���_...");
                            key.SetValue("BackupProductKeyDefault", "W269N-WFGWX-YVC9B-4J6C9-T83GX", RegistryValueKind.String);
                        }
                        addMessageToListBox("���b�ҥ�Windows...");
                        ExecuteCmd("slmgr -ato");
                        addMessageToListBox("Windows�ҥΧ����I");
                    }
                    catch (Exception ex)
                    {
                        addMessageToListBox($"����ɵo�Ϳ��~�G{ex.Message}");
                    }
                }));
            }
            if (checkedListBox1.CheckedItems.Contains("    ���ୱ�ϥ�"))
            {
                addMessageToListBox("���b���ୱ�ϥ�...");
                tasks.Add(Task.Run(() =>
                {
                    enableDesktopIcons();
                    addMessageToListBox("�ୱ�ϥܧ�粒���I");
                }));
            }
            if (checkedListBox1.CheckedItems.Contains("    ���q���ﶵ"))
            {
                addMessageToListBox("���b���q���ﶵ...");
                tasks.Add(Task.Run(() =>
                {
                    ExecuteCmd(@"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c && powercfg /change standby-timeout-ac 0 && powercfg /change monitor-timeout-ac 0");
                    addMessageToListBox("�q���ﶵ��粒���I");
                }));
            }
            foreach (var installer in config.Installers)
            {
                if (checkedListBox1.CheckedItems.Contains(installer.Name))
                {
                    string url;
                    if (!string.IsNullOrEmpty(installer.DownloadURL))
                    {
                        url = $"{installer.DownloadURL}/{installer.File}";
                    }
                    else
                    {
                        url = $"{baseURL}/{installer.File}";
                    }
                    string filePath = $"./temp/{installer.File}";
                    downloadAndInstall(tasks, url, filePath, installer.Name);
                }
            }
            await Task.WhenAll(tasks);
            addMessageToListBox("���b�M��...");
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                    addMessageToListBox("�M�������C");
                }
                catch (Exception ex)
                {
                    addMessageToListBox($"�M���ɵo�Ϳ��~�G{ex.Message}");
                }
            }
            addMessageToListBox("�Ҧ��w�˶��جҤw�����C");
            button1.Enabled = true;
            checkedListBox1.Enabled = true;
        }
        public void addMessageToListBox(string message)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() =>
                {
                    string currentTime = DateTime.Now.ToString("HH:mm:ss");
                    listBox1.Items.Add($"[{currentTime}] {message}");

                    if (listBox1.Items.Count > 0)
                        listBox1.TopIndex = listBox1.Items.Count - 1;
                }));
                return;
            }
            string currentTime = DateTime.Now.ToString("HH:mm:ss");
            listBox1.Items.Add($"[{currentTime}] {message}");
            if (listBox1.Items.Count > 0)
                listBox1.TopIndex = listBox1.Items.Count - 1;
        }

        public static bool IsProgramInstalled(string programName)
        {
            if (IsProgramInRegistry(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", programName) || IsProgramInRegistry(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", programName))
                return true;

            return false;
        }

        private static bool IsProgramInRegistry(string registryPath, string programName)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                if (key != null)
                {
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                        {
                            object displayName = subKey?.GetValue("DisplayName");
                            if (displayName != null && displayName.ToString().Contains(programName, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private void enableDesktopIcons()
        {
            string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key == null)
                    {
                        addMessageToListBox("����ɵo�Ϳ��~�G�L�k�s�����U����");
                        return;
                    }

                    key.SetValue("{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, RegistryValueKind.DWord);
                    key.SetValue("{59031a47-3f72-44a7-89c5-5595fe6b30ee}", 0, RegistryValueKind.DWord);
                    key.SetValue("{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}", 0, RegistryValueKind.DWord);
                    key.SetValue("{645FF040-5081-101B-9F08-00AA002F954E}", 0, RegistryValueKind.DWord);
                }

                SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                addMessageToListBox($"����ɵo�Ϳ��~�G{ex.Message}");
            }
        }

        void downloadAndInstall(List<Task> tasks, string url, string filePath, string softwareName)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    addMessageToListBox($"���b�U��{softwareName}�w����...");
                    download(url, filePath);
                }
                catch (Exception)
                {
                    addMessageToListBox($"�U�� {url} �ɵo�Ϳ��~");
                    return;
                }
                addMessageToListBox($"{softwareName}�w���ɤU�������A���b����w�˵{��...");
                runInstaller(filePath);
                addMessageToListBox($"{softwareName}�w�˧����I");
            }));
        }

        void download(string url, string destinationPath)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                using (FileStream fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                }
            }
        }

        void runInstaller(string filePath)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath
                }
            };

            process.Start();
            process.WaitForExit();
        }
    }
}