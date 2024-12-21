using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace installTool
{
    public class Installer
    {
        public string Name { get; set; }
        public string File { get; set; }
        public bool SetDefaultEnable { get; set; } = false;
        public string DownloadURL { get; set; }
    }

    public class InstallerConfig
    {
        public List<Installer> Installers { get; set; }
        public string DownloadURL { get; set; }
    }

    public class YamlReader
    {
        public InstallerConfig ReadYamlFile(string filePath)
        {
            InstallerConfig installerConfig;
            if (File.Exists(filePath))
            {
                var yamlContent = File.ReadAllText(filePath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                installerConfig = deserializer.Deserialize<InstallerConfig>(yamlContent);
            }
            else
            {
                installerConfig = GenerateDefaultConfig();
                WriteYamlFile(filePath, installerConfig);
            }

            return installerConfig;
        }

        public InstallerConfig GenerateDefaultConfig()
        {
            return new InstallerConfig
            {
                Installers = new List<Installer>
                {
                     new Installer { Name = "Chrome", File = "ChromeSetup.exe"},
                     new Installer { Name = "Office 2021", File = "OFFICE2021.exe" },
                     new Installer { Name = "Armoury Crate", File = "ArmouryCrateInstaller.exe" },
                     new Installer { Name = "ID - COOLING驅動", File = "ID-COOLING2.1V1.0.3.msi" },
                     new Installer { Name = "瓦爾基里驅動", File = "VK.exe" },
                     new Installer { Name = "Steam", File = "SteamSetup.exe" },
                },
                DownloadURL = "http://192.168.1.1"
            };
        }

        public void WriteYamlFile(string filePath, InstallerConfig config)
        {
            var serializer = new SerializerBuilder()
                             .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                             .Build();

            var writer = new StringWriter();

            writer.WriteLine("# 安裝工具配置檔案");
            writer.WriteLine("");
            writer.WriteLine("# 下載路徑");
            writer.WriteLine($"downloadURL: {config.DownloadURL}");
            writer.WriteLine("");
            writer.WriteLine("# 以下列出的是需要安裝的程式及其安裝檔案名稱");
            writer.WriteLine("# 若需要新增或修改安裝程式，請根據以下格式進行操作");
            writer.WriteLine("# 參數說明:");
            writer.WriteLine("#             name: 必填，顯示於GUI及紀錄欄的名稱");
            writer.WriteLine("#             file: 必填，下載路徑的檔案名稱，其完整路徑為 downloadURL/file");
            writer.WriteLine("# setDefaultEnable: 選填，是否預設為勾選，預設為否");
            writer.WriteLine("#      downloadURL: 選填，若填入該值，將會覆蓋全域下載路徑");
            writer.WriteLine("");
            writer.WriteLine("installers:");
            foreach (var installer in config.Installers)
            {
                writer.WriteLine($"  - name: {installer.Name}");
                writer.WriteLine($"    file: {installer.File}");
                if (installer.SetDefaultEnable)
                    writer.WriteLine($"    setDefaultEnable: true");
                else
                    writer.WriteLine($"    setDefaultEnable: false");
                writer.WriteLine($"    downloadURL: {installer.DownloadURL}");
            }

            File.WriteAllText(filePath, writer.ToString());
        }
    }
}
