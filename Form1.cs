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
                addMessageToListBox("設定檔錯誤：downloadURL 值為空");
                checkedListBox1.Enabled = false;
                return;
            }

            foreach (var installer in config.Installers)
            {
                if (string.IsNullOrEmpty(installer.Name))
                {
                    addMessageToListBox("設定檔錯誤：installers.name 值為空");
                    checkedListBox1.Enabled = false;
                    return;
                }
                if (string.IsNullOrEmpty(installer.File))
                {
                    addMessageToListBox("設定檔錯誤：installers.file 值為空");
                    checkedListBox1.Enabled = false;
                    return;
                }
                if (installer.SetDefaultEnable == null)
                {
                    addMessageToListBox("設定檔錯誤：installers.setDefaultEnable 值為空");
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
                addMessageToListBox("偵測到未安裝Chrome，已自動勾選該選項。");
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

                if (checkedListBox1.Items[index].ToString() == "啟用Windows" && !isChecked)
                {
                    checkedListBox1.SetItemChecked(1, true);
                    checkedListBox1.SetItemChecked(2, true);
                }
                else if (checkedListBox1.Items[index].ToString() == "啟用Windows" && isChecked)
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
                addMessageToListBox("執行失敗：" + e.Message);
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
                addMessageToListBox("生成暫存檔...");
                Directory.CreateDirectory(folderPath);
            }
            if (checkedListBox1.CheckedItems.Contains("啟用Windows"))
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform", writable: true))
                        {
                            if (key == null) 
                            { 
                                addMessageToListBox("執行時發生錯誤：無法存取註冊表鍵");
                                return;
                            }
                            addMessageToListBox("正在更改註冊表：KMS伺服器...");
                            key.SetValue("KeyManagementServiceName", "skms.netnr.eu.org", RegistryValueKind.String);
                            addMessageToListBox("正在更改註冊表：KMS金鑰...");
                            key.SetValue("BackupProductKeyDefault", "W269N-WFGWX-YVC9B-4J6C9-T83GX", RegistryValueKind.String);
                        }
                        addMessageToListBox("正在啟用Windows...");
                        ExecuteCmd("slmgr -ato");
                        addMessageToListBox("Windows啟用完成！");
                    }
                    catch (Exception ex)
                    {
                        addMessageToListBox($"執行時發生錯誤：{ex.Message}");
                    }
                }));
            }
            if (checkedListBox1.CheckedItems.Contains("    更改桌面圖示"))
            {
                addMessageToListBox("正在更改桌面圖示...");
                tasks.Add(Task.Run(() =>
                {
                    enableDesktopIcons();
                    addMessageToListBox("桌面圖示更改完成！");
                }));
            }
            if (checkedListBox1.CheckedItems.Contains("    更改電源選項"))
            {
                addMessageToListBox("正在更改電源選項...");
                tasks.Add(Task.Run(() =>
                {
                    ExecuteCmd(@"powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c && powercfg /change standby-timeout-ac 0 && powercfg /change monitor-timeout-ac 0");
                    addMessageToListBox("電源選項更改完成！");
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
            addMessageToListBox("正在清除...");
            if (Directory.Exists(folderPath))
            {
                try
                {
                    Directory.Delete(folderPath, true);
                    addMessageToListBox("清除完成。");
                }
                catch (Exception ex)
                {
                    addMessageToListBox($"清除時發生錯誤：{ex.Message}");
                }
            }
            addMessageToListBox("所有安裝項目皆已完成。");
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
                        addMessageToListBox("執行時發生錯誤：無法存取註冊表鍵");
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
                addMessageToListBox($"執行時發生錯誤：{ex.Message}");
            }
        }

        void downloadAndInstall(List<Task> tasks, string url, string filePath, string softwareName)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    addMessageToListBox($"正在下載{softwareName}安裝檔...");
                    download(url, filePath);
                }
                catch (Exception)
                {
                    addMessageToListBox($"下載 {url} 時發生錯誤");
                    return;
                }
                addMessageToListBox($"{softwareName}安裝檔下載完成，正在執行安裝程式...");
                runInstaller(filePath);
                addMessageToListBox($"{softwareName}安裝完成！");
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