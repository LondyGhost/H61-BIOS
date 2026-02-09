using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;

class Program : Form {
    private Button btnStart;
    private Label lblStatus;
    private ProgressBar progressBar;

    public Program() {
        this.Text = "H61 BIOS 固件更新工具";
        this.Size = new Size(400, 220);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        lblStatus = new Label() { Text = "注意此程序为H61主板，确认好主板再点击开始更新", Top = 30, Left = 50, Size = new Size(300, 20) };
        progressBar = new ProgressBar() { Top = 60, Left = 50, Size = new Size(300, 25), Style = ProgressBarStyle.Marquee, Visible = false };
        btnStart = new Button() { Text = "开始刷写 BIOS", Top = 110, Left = 130, Size = new Size(120, 40) };

        btnStart.Click += (s, e) => StartFlash();

        this.Controls.Add(lblStatus);
        this.Controls.Add(progressBar);
        this.Controls.Add(btnStart);
    }

    private void StartFlash() {
        var result = MessageBox.Show("警告：刷写 BIOS 具有风险，请确保电源稳定！\n\n确定要继续吗？", "最后确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (result != DialogResult.OK) return;

        btnStart.Enabled = false;
        lblStatus.Text = "正在解压固件组件...";
        progressBar.Visible = true;

        string tempPath = Path.Combine(Path.GetTempPath(), "BIOS_Update_Temp");
        if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

        try {
            string[] files = { "fptw64.exe", "bios.bin", "fparts.txt", "fptcfg.ini", "idrvdll32e.dll", "pmxdll32e.dll" };
            foreach (var file in files) {
                ExtractResource(file, Path.Combine(tempPath, file));
            }

            lblStatus.Text = "正在进行底层刷写，请勿断电...";

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Path.Combine(tempPath, "fptw64.exe");
            psi.Arguments = "-F bios.bin";
            psi.WorkingDirectory = tempPath;
            psi.Verb = "runas"; 
            psi.UseShellExecute = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden; 

            Process p = Process.Start(psi);
            p.EnableRaisingEvents = true;
            p.Exited += (s, e) => {
                this.Invoke((Action)(() => {
                    progressBar.Visible = false;
                    if (p.ExitCode == 0) {
                        lblStatus.Text = "更新成功！";
                        MessageBox.Show("BIOS 已成功刷入，请重启以应用更改。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // --- 关键改动：用户点击 OK 后关闭主窗口 ---
                        this.Close(); 
                    } else {
                        lblStatus.Text = "刷写失败，代码: " + p.ExitCode;
                        MessageBox.Show("刷写过程中出现错误，请检查 DMI 锁定状态。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnStart.Enabled = true; // 失败时不退出，允许用户查看错误码或重试
                    }
                }));
            };
        } catch (Exception ex) {
            MessageBox.Show("启动失败: " + ex.Message);
            btnStart.Enabled = true;
        }
    }

    static void ExtractResource(string resName, string path) {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
        using (FileStream fs = new FileStream(path, FileMode.Create)) {
            s.CopyTo(fs);
        }
    }

    [STAThread]
    static void Main() {
        Application.EnableVisualStyles();
        Application.Run(new Program());
    }
}
