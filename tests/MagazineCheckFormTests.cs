// 用途：验证检查弹匣设置窗口的组合键录入、时长编辑、取消与保存，不发送全局键鼠操作。
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using TarkovCrosshair;

public static class MagazineCheckFormTests
{
    // 每一项对应用户能直接操作或看到的设置结果。
    private static int checks;

    /// <summary>通过真实窗口控件验证编辑，并可输出默认界面截图供查看。</summary>
    public static int Run(string screenshotPath)
    {
        checks = 0;
        KeyBindings original = new KeyBindings();
        using (MagazineCheckForm form = new MagazineCheckForm(original))
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-30000, -30000);
            form.Show();
            Application.DoEvents();
            Check(form.SelectedKey == 82 && form.SelectedModifiers == KeyChord.Control && form.SelectedSeconds == 3,
                "初始窗口显示 Ctrl+R 和三秒");
            Capture(form, Keys.Alt | Keys.Shift | Keys.F10);
            Check(form.SelectedKey == 121 && form.SelectedModifiers == (KeyChord.Alt | KeyChord.Shift), "录入完整的 Alt+Shift+F10");
            NumericUpDown duration = (NumericUpDown)FindNamed(form, "magazineCheckSeconds");
            duration.Value = 4.5m;
            Check(form.SelectedSeconds == 4.5, "时长可设置为小数秒");
            Check(original.MagazineCheckKey == 82 && original.MagazineCheckBlockSeconds == 3, "编辑过程中不改变原配置");
            Button key = (Button)FindNamed(form, "magazineCheckKey");
            key.PerformClick();
            SendKey(form, Keys.ControlKey);
            Check(key.Text.Contains("请按"), "只按修饰键时继续等待主键");
            SendKey(form, Keys.Control | Keys.I);
            Check(form.SelectedKey == 73 && form.SelectedModifiers == KeyChord.Control, "修饰键之后的主键完成录入");
            key.PerformClick();
            SendKey(form, Keys.Escape);
            Check(form.SelectedKey == 73 && form.Visible, "Esc 只取消录入，不关闭窗口或改动组合");
            Capture(form, Keys.Tab);
            Check(form.SelectedKey == 9 && form.SelectedModifiers == 0, "Tab 可作为没有修饰键的主键");
            duration.Value = 0;
            Check(form.SelectedSeconds == 0, "零秒关闭检查保护");
            FindButton(form, "恢复默认").PerformClick();
            Check(form.SelectedKey == 82 && form.SelectedModifiers == KeyChord.Control && form.SelectedSeconds == 3,
                "恢复默认同时恢复组合和时长");
            if (!string.IsNullOrEmpty(screenshotPath)) SavePreview(form, screenshotPath);
            FindButton(form, "取消").PerformClick();
            Check(form.DialogResult == DialogResult.Cancel && original.MagazineCheckKey == 82, "取消不修改原配置");
        }
        using (MagazineCheckForm form = new MagazineCheckForm(original))
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-30000, -30000);
            form.Show();
            Capture(form, Keys.Control | Keys.F9);
            ((NumericUpDown)FindNamed(form, "magazineCheckSeconds")).Value = 2.5m;
            FindButton(form, "保存").PerformClick();
            Check(form.DialogResult == DialogResult.OK && form.SelectedKey == 120 && form.SelectedSeconds == 2.5,
                "保存返回完整编辑结果");
        }
        Check(MagazineCheckForm.Describe(original) == "Ctrl+R · 3 秒", "按键页摘要包含完整组合和时长");
        original.MagazineCheckBlockSeconds = 0;
        Check(MagazineCheckForm.Describe(original) == "已关闭", "按键页明确显示关闭状态");
        return checks;
    }

    /// <summary>点击真实录入按钮，再调用窗口相同的按键处理入口。</summary>
    private static void Capture(Form form, Keys combination)
    {
        ((Button)FindNamed(form, "magazineCheckKey")).PerformClick();
        SendKey(form, combination);
    }

    /// <summary>直接提交含修饰键的消息，不依赖测试机器上实际按住了哪些键。</summary>
    private static void SendKey(Form form, Keys combination)
    {
        Message message = Message.Create(form.Handle, 0x100, new IntPtr((int)(combination & Keys.KeyCode)), IntPtr.Zero);
        MethodInfo handler = typeof(MagazineCheckForm).GetMethod("ProcessCmdKey", BindingFlags.Instance | BindingFlags.NonPublic);
        object[] arguments = new object[] { message, combination };
        Check((bool)handler.Invoke(form, arguments), "窗口消费录入按键：" + combination);
    }

    /// <summary>按控件名称查找编辑字段，名称缺失时立即报告。</summary>
    private static Control FindNamed(Control parent, string name)
    {
        Control[] matches = parent.Controls.Find(name, true);
        if (matches.Length != 1) throw new Exception("找不到唯一控件：" + name);
        return matches[0];
    }

    /// <summary>按用户看到的文字查找操作按钮。</summary>
    private static Button FindButton(Control parent, string text)
    {
        foreach (Control child in parent.Controls)
        {
            Button button = child as Button;
            if (button != null && button.Text == text) return button;
            Button nested = FindButton(child, text);
            if (nested != null) return nested;
        }
        return null;
    }

    /// <summary>仅把窗口内容绘制到图片，不截取桌面或游戏画面。</summary>
    private static void SavePreview(Form form, string path)
    {
        using (Bitmap preview = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
            preview.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    /// <summary>断言包含具体界面操作，便于查找录入或布局问题。</summary>
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
}
