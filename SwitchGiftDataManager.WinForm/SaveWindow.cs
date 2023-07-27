using System.IO;
using SwitchGiftDataManager.Core;
using Enums;

namespace SwitchGiftDataManager.WinForm;

public partial class SaveWindow : Form
{
    private BCATManager Package;
    private Games Game;

    public SaveWindow(BCATManager bcat, Games game) 
    { 
        InitializeComponent();

        Package = bcat;
        Game = game;

        if (Game is Games.LGPE)
        {
            RadioUnique.Checked = true;
            RadioMultiple.Enabled = false;
        }
        else if (Game is Games.BDSP)
            RadioUnique.Enabled = false;
    }

    private void BtnCancel_Click(object sender, EventArgs e) => this.Close();

    private void BtnSrcBrowse_Click(object sender, EventArgs e)
    {
        if(FolderBrowser.ShowDialog() == DialogResult.OK)
        {
            TxtSourcePath.Text = FolderBrowser.SelectedPath;
            TxtDestPath.Text = Path.GetDirectoryName(FolderBrowser.SelectedPath);
        }
    }

    private void BtnPath_Click(object sender, EventArgs e)
    {
        if (FolderBrowser.ShowDialog() == DialogResult.OK)
            TxtDestPath.Text = FolderBrowser.SelectedPath;
    }

    private void BtnSave_Click(object sender, EventArgs e)
    {
        if(!RadioUnique.Checked && !RadioMultiple.Checked)
        {
            MessageBox.Show("选择生成方法.");
            return;
        }
        if (!CheckValidBcatPath(TxtSourcePath.Text))
        {
            MessageBox.Show("BCAT源头路径无效");
            return;
        }
        if (!CheckValidPath(TxtDestPath.Text))
        {
            MessageBox.Show("无效的目标路径.");
            return;
        }

        var path = Path.Combine(TxtDestPath.Text, $"Forged_BCAT_{Game}");
        CopyDirectory(TxtSourcePath.Text, path);

        if (RadioMultiple.Checked)
        {
            try
            {
                var wcdata = Package.ConcatenateFiles();
                var metadata = Package.ForgeMetaInfo(wcdata.ToArray());
                var metadatapath = Path.Combine(path, "目录");
                metadatapath = Path.Combine(metadatapath, Package.GetDefaultBcatFolderName());
                var wcpath = Path.Combine(metadatapath, "文件");

                if (Directory.Exists(metadatapath))
                    DeleteFilesAndDirectory(metadatapath);

                Directory.CreateDirectory(wcpath);
                File.WriteAllBytes(Path.Combine(metadatapath, "元文件"), metadata.ToArray());
                File.WriteAllBytes(Path.Combine(wcpath, Package.GetDefaultBcatFileName()), wcdata.ToArray());
                MessageBox.Show($"Saved in {path}{Environment.NewLine}BCAT forge was successful.");
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Internal Error");
                this.Close();
            }
        }
        else
        {
            var metadata = Package.ForgeMetaInfo();
            var metadatapath = Path.Combine(path, "directories");
            metadatapath = Path.Combine(metadatapath, Package.GetDefaultBcatFolderName());
            var wcspath = Path.Combine(metadatapath, "文件");

            if (Directory.Exists(metadatapath))
                DeleteFilesAndDirectory(metadatapath);

            Directory.CreateDirectory(wcspath);
            File.WriteAllBytes(Path.Combine(metadatapath, "元文件"), metadata.ToArray());
            if (Package.TrySaveAllWondercards(wcspath))
            {
                MessageBox.Show($"保存于{path}{Environment.NewLine}BCAT保存信息成功.");
                this.Close();
            }
            else
            {
                MessageBox.Show("内部错误.");
                this.Close();
            }
        }
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

    private static bool CheckValidPath(string path)
    {
        if(string.IsNullOrWhiteSpace(path))
            return false;

        if (!Directory.Exists(path))
            return false;

        return true;
    }

    private static void CopyDirectory(string source, string dest)
    {
        var dir = new DirectoryInfo(source);
        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(dest);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(dest, file.Name);
            if(!File.Exists(targetFilePath))
                file.CopyTo(targetFilePath);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(dest, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private void DeleteFilesAndDirectory(string targetDir)
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
}