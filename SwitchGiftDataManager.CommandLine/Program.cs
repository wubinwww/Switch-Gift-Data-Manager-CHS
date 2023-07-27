using SwitchGiftDataManager.Core;
using Enums;

namespace SwitchGiftDataManager.CommandLine;

public static class Program
{
    public static void Main()
    {
        var msg = $"Switch神秘礼物数据管理器 v{BCATManager.Version}";
        Log(msg);

        Task.Run(TryUpdate).Wait();

        msg = $"{Environment.NewLine}选择你的游戏:{Environment.NewLine}{Environment.NewLine}" +
            $"1 - 去皮去伊{Environment.NewLine}" +
            $"2 - 剑盾{Environment.NewLine}" +
            $"3 - 珍珠钻石{Environment.NewLine}" +
            $"4 - 阿尔宙斯{Environment.NewLine}" +
            $"5 - 朱紫";
        Log(msg);

        Games game = (Games)int.Parse(Console.ReadLine()!);
        if (game is Games.None || game > Games.SCVI)
        {
            Log("无效的输入，已终止。");
            Console.ReadKey();
            return;
        }

        var bcat = new BCATManager(game);

        msg = $"{Environment.NewLine}输入一个有效的输入路径.{Environment.NewLine}{Environment.NewLine}路径可以是任意一个:{Environment.NewLine}" +
            $"- 到神秘礼物卡片文件的直接(完整)路径{Environment.NewLine}" +
            $"- 包含神秘礼物卡片文件的文件夹的(完整)路径";
        Log(msg);

        var path = Console.ReadLine()!;
        if (File.Exists(path))
            bcat.TryAddWondercards(File.ReadAllBytes(path));
        else if (CheckValidPath(path))
            foreach (var file in Directory.GetFiles(path))
                if (!bcat.TryAddWondercards(File.ReadAllBytes(file)))
                    Log($"{file} 无法加载.");

        if (bcat.Count() <= 0)
        {
            Log("没有加载有效文件，已终止.");
            Console.ReadKey();
            return;
        }

        bcat.Sort();
        Log($"{Environment.NewLine}输入转储BCAT的源(完整)路径:");
        var sourcepath = Console.ReadLine()!;
        if (!CheckValidBcatPath(sourcepath))
        {
            Log("不是有效的BCAT文件夹路径，已终止.");
            Console.ReadKey();
            return;
        }

        Log($"{Environment.NewLine}输入保存伪造BCAT的目标(完整)路径:");
        var destpath = Console.ReadLine()!;
        if (!CheckValidPath(destpath))
        {
            Log("不是一个有效的路径，已终止.");
            Console.ReadKey();
            return;
        }

        if (game is not (Games.LGPE or Games.BDSP))
        {
            msg = $"{Environment.NewLine}选择一个保存选项:{Environment.NewLine}{Environment.NewLine}" +
                $"1 - 合并为一个文件{Environment.NewLine}" +
                $"2 - 单独保存文件";
            Log(msg);
        }

        var opt = game switch {
            Games.LGPE => 2,
            Games.BDSP => 1,
            _ => int.Parse(Console.ReadLine()!),
        };

        if(opt < 1 || opt > 2)
        {
            Log("无效的输入，已终止。");
            Console.ReadKey();
            return;
        }

        destpath = Path.Combine(destpath, $"Forged_BCAT_{game}");
        CopyDirectory(sourcepath, destpath);

        if (opt == 1)
        {
            try
            {
                var wcdata = bcat.ConcatenateFiles();
                var metadata = bcat.ForgeMetaInfo(wcdata.ToArray());
                var metadatapath = Path.Combine(destpath, "目录");
                metadatapath = Path.Combine(metadatapath, bcat.GetDefaultBcatFolderName());
                var wcpath = Path.Combine(metadatapath, "文件");

                if (Directory.Exists(metadatapath))
                    DeleteFilesAndDirectory(metadatapath);

                Directory.CreateDirectory(wcpath);
                File.WriteAllBytes(Path.Combine(metadatapath, "元文件"), metadata.ToArray());
                File.WriteAllBytes(Path.Combine(wcpath, bcat.GetDefaultBcatFileName()), wcdata.ToArray());
                Log($"BCAT伪造成功后，保存在{path}{Environment.NewLine}.{Environment.NewLine}按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception)
            {
                Log("内部错误，按任意键退出...");
                Console.ReadKey();
            }
        }
        else
        {
            var metadata = bcat.ForgeMetaInfo();
            var metadatapath = Path.Combine(destpath, "目录");
            metadatapath = Path.Combine(metadatapath, bcat.GetDefaultBcatFolderName());
            var wcspath = Path.Combine(metadatapath, "文件");

            if (Directory.Exists(metadatapath))
                DeleteFilesAndDirectory(metadatapath);

            Directory.CreateDirectory(wcspath);
            File.WriteAllBytes(Path.Combine(metadatapath, "元文件"), metadata.ToArray());
            if (bcat.TrySaveAllWondercards(wcspath))
            {
                Log($"BCAT伪造成功后，保存在{path}{Environment.NewLine}.{Environment.NewLine}按任意键退出...");
                Console.ReadKey();
            }
            else
            {
                Log("内部错误，按任意键退出...");
                Console.ReadKey();
            }
        }

        return;
    }

    private static bool CheckValidPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!Directory.Exists(path))
            return false;

        return true;
    }

    private static bool CheckValidBcatPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (!Directory.Exists(Path.Combine(path, "directories")))
            return false;

        if (!File.Exists(Path.Combine(path, "directories.meta")))
            return false;

        if (!File.Exists(Path.Combine(path, "etag.bin")))
            return false;

        if (!File.Exists(Path.Combine(path, "list.msgpack")))
            return false;

        if (!File.Exists(Path.Combine(path, "na_required")))
            return false;

        if (!File.Exists(Path.Combine(path, "passphrase.bin")))
            return false;

        return true;
    }

    static void CopyDirectory(string source, string dest)
    {
        var dir = new DirectoryInfo(source);
        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(dest);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(dest, file.Name);
            if (!File.Exists(targetFilePath))
                file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(dest, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private static void DeleteFilesAndDirectory(string targetDir)
    {
        string[] files = Directory.GetFiles(targetDir);
        string[] dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
            DeleteFilesAndDirectory(dir);

        Directory.Delete(targetDir, false);
    }

    private static async Task TryUpdate()
    {
        if (await GitHubUtil.IsUpdateAvailable())
        {
            Log("程序更新可用。是否要下载最新版本?\n[Y\\n]:");
            var str = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(str) && (str.ToLower().Equals("y") || str.ToLower().Equals("yes")))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = @"https://github.com/ZiYuKing/Switch-Gift-Data-Manager-CHS", UseShellExecute = true });
        }
    }

    private static void Log(string msg) => Console.WriteLine(msg);
}