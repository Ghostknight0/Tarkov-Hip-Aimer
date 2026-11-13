// 用途：配置检查弹匣的组合键和忽略开镜时间，编辑取消时不改变当前设置。
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TarkovCrosshair
{
    internal sealed class MagazineCheckForm : Form
    {
        // 窗口只编辑自己的副本，保存后由设置页发布到渲染线程。
        private int mainKey;
        private bool capturing, suppressNextClick;
        private readonly Button captureButton = new Button();
        private readonly Button stopButton = new Button();
        private readonly CheckBox ctrl = new CheckBox(), alt = new CheckBox(), shift = new CheckBox();
        private readonly NumericUpDown seconds = new NumericUpDown();
        private readonly Label prompt = new Label();

        public int SelectedKey { get { return mainKey; } }
        public double SelectedSeconds { get { return (double)seconds.Value; } }
        public int SelectedModifiers
        {
            get { return (ctrl.Checked ? KeyChord.Control : 0) | (alt.Checked ? KeyChord.Alt : 0) | (shift.Checked ? KeyChord.Shift : 0); }
        }

        /// <summary>使用独立窗口容纳录入、修饰键和时长，避免挤占原按键页。</summary>
        public MagazineCheckForm(KeyBindings current)
        {
            Text = "检查弹匣保护";
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(560, 305);
            MinimumSize = new Size(540, 330);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            KeyPreview = true;
            BuildContent();
            LoadValues(current);
            Theme.Apply(this);
        }

        /// <summary>按设置页显示组合键；零秒时明确显示功能已关闭。</summary>
        public static string Describe(KeyBindings keys)
        {
            if (keys.MagazineCheckBlockSeconds <= 0) return "已关闭";
            string text = "";
            if ((keys.MagazineCheckModifiers & KeyChord.Control) != 0) text += "Ctrl+";
            if ((keys.MagazineCheckModifiers & KeyChord.Alt) != 0) text += "Alt+";
            if ((keys.MagazineCheckModifiers & KeyChord.Shift) != 0) text += "Shift+";
            return text + SettingsForm.KeyName(keys.MagazineCheckKey) + " · " + keys.MagazineCheckBlockSeconds.ToString("0.#") + " 秒";
        }

        /// <summary>通过布局容器安排说明和按钮，在系统缩放下保留完整文字。</summary>
        private void BuildContent()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(14);
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);
            Label hint = new Label();
            hint.AutoSize = true;
            hint.MaximumSize = new Size(520, 0);
            hint.Text = "检查触发后显示瞄准点，在设定时间内忽略开镜点击。\n请与游戏里的检查弹匣按键保持一致。";
            hint.Margin = new Padding(0, 0, 0, 12);
            layout.Controls.Add(hint, 0, 0);
            TableLayoutPanel fields = new TableLayoutPanel();
            fields.AutoSize = true;
            fields.ColumnCount = 2;
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            captureButton.Name = "magazineCheckKey";
            captureButton.AutoSize = true;
            captureButton.MinimumSize = new Size(170, 28);
            captureButton.Click += delegate
            {
                if (suppressNextClick) { suppressNextClick = false; return; }
                SetCapturing(true);
            };
            captureButton.MouseDown += CaptureMouse;
            AddField(fields, "主键", captureButton);
            FlowLayoutPanel modifiers = ButtonLine();
            ctrl.Text = "Ctrl"; alt.Text = "Alt"; shift.Text = "Shift";
            ctrl.Name = "magazineCheckCtrl"; alt.Name = "magazineCheckAlt"; shift.Name = "magazineCheckShift";
            foreach (CheckBox modifier in new CheckBox[] { ctrl, alt, shift })
            {
                modifier.AutoSize = true;
                modifiers.Controls.Add(modifier);
            }
            AddField(fields, "同时按住", modifiers);
            FlowLayoutPanel duration = ButtonLine();
            seconds.Name = "magazineCheckSeconds";
            seconds.Minimum = 0; seconds.Maximum = 30;
            seconds.DecimalPlaces = 1; seconds.Increment = 0.1m; seconds.Width = 90;
            duration.Controls.Add(seconds);
            Label unit = new Label();
            unit.AutoSize = true; unit.Text = "秒（0 表示关闭）"; unit.Padding = new Padding(0, 5, 0, 0);
            duration.Controls.Add(unit);
            AddField(fields, "忽略开镜", duration);
            layout.Controls.Add(fields, 0, 1);
            prompt.AutoSize = true;
            prompt.MaximumSize = new Size(520, 0);
            prompt.Margin = new Padding(0, 10, 0, 8);
            prompt.MouseDown += CaptureMouse;
            layout.Controls.Add(prompt, 0, 2);
            FlowLayoutPanel actions = ButtonLine();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            Button save = ActionButton("保存", delegate { SetCapturing(false); DialogResult = DialogResult.OK; });
            Button cancel = ActionButton("取消", delegate { DialogResult = DialogResult.Cancel; });
            actions.Controls.Add(save); actions.Controls.Add(cancel);
            actions.Controls.Add(ActionButton("恢复默认", delegate { LoadValues(new KeyBindings()); }));
            stopButton.Text = "取消录入"; stopButton.AutoSize = true;
            stopButton.Click += delegate { SetCapturing(false); };
            actions.Controls.Add(stopButton);
            AcceptButton = save; CancelButton = cancel;
            layout.Controls.Add(actions, 0, 3);
        }

        /// <summary>统一采用自动宽度按钮，避免文字随系统字体被截断。</summary>
        private static Button ActionButton(string text, Action action)
        {
            Button button = new Button();
            button.Text = text; button.AutoSize = true;
            button.Click += delegate { action(); };
            return button;
        }

        /// <summary>小组内的控件保持在一行，整体由表格排列。</summary>
        private static FlowLayoutPanel ButtonLine()
        {
            FlowLayoutPanel line = new FlowLayoutPanel();
            line.AutoSize = true; line.WrapContents = false; line.Margin = new Padding(0);
            return line;
        }

        /// <summary>为每个字段添加左侧名称，控件靠左并上下居中。</summary>
        private static void AddField(TableLayoutPanel fields, string name, Control control)
        {
            int row = fields.RowCount++;
            Label label = new Label();
            label.Text = name; label.AutoSize = true; label.Anchor = AnchorStyles.Left;
            label.Margin = new Padding(0, 5, 18, 5);
            control.Anchor = AnchorStyles.Left;
            fields.Controls.Add(label, 0, row); fields.Controls.Add(control, 1, row);
        }

        /// <summary>恢复默认只修改窗口副本，按取消仍可放弃。</summary>
        private void LoadValues(KeyBindings current)
        {
            KeyBindings copy = current.Clone();
            copy.Validate();
            mainKey = copy.MagazineCheckKey;
            SetModifiers(copy.MagazineCheckModifiers);
            seconds.Value = (decimal)copy.MagazineCheckBlockSeconds;
            SetCapturing(false);
        }

        /// <summary>同步显示完整组合键中要求按住的修饰键。</summary>
        private void SetModifiers(int modifiers)
        {
            ctrl.Checked = (modifiers & KeyChord.Control) != 0;
            alt.Checked = (modifiers & KeyChord.Alt) != 0;
            shift.Checked = (modifiers & KeyChord.Shift) != 0;
        }

        /// <summary>录入仅在本窗口生效，Esc 或按钮可退出录入。</summary>
        private void SetCapturing(bool value)
        {
            capturing = value;
            stopButton.Enabled = value;
            captureButton.Text = value ? "请按组合键…" : SettingsForm.KeyName(mainKey) + "（点击录入）";
            prompt.Text = value ? "按下完整组合键；Esc 取消录入。鼠标键请点上方主键按钮。"
                : "仅用于“点一下切换”开镜。时间结束后，需松开开镜键再重新按下。";
        }

        /// <summary>先按 Ctrl/Alt/Shift 不会结束录入，收到主键后才保存完整组合。</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!capturing) return base.ProcessCmdKey(ref msg, keyData);
            int key = (int)(keyData & Keys.KeyCode);
            if (key == 27) { SetCapturing(false); return true; }
            if (key < 1 || key > 254 || KeyChord.IsModifier(key)) return true;
            FinishCapture(key, keyData);
            return true;
        }

        /// <summary>键盘和鼠标录入都保存当时真正按住的修饰键。</summary>
        private void FinishCapture(int key, Keys keyData)
        {
            int modifiers = 0;
            if ((keyData & Keys.Control) != 0) modifiers |= KeyChord.Control;
            if ((keyData & Keys.Alt) != 0) modifiers |= KeyChord.Alt;
            if ((keyData & Keys.Shift) != 0) modifiers |= KeyChord.Shift;
            mainKey = key;
            SetModifiers(modifiers);
            SetCapturing(false);
        }

        /// <summary>鼠标主键沿用普通按钮录入方式，不监听其他程序的鼠标事件。</summary>
        private void CaptureMouse(object sender, MouseEventArgs e)
        {
            if (!capturing) return;
            int key = 0;
            if (e.Button == MouseButtons.Left) key = 1;
            else if (e.Button == MouseButtons.Right) key = 2;
            else if (e.Button == MouseButtons.Middle) key = 4;
            else if (e.Button == MouseButtons.XButton1) key = 5;
            else if (e.Button == MouseButtons.XButton2) key = 6;
            if (key == 0) return;
            suppressNextClick = sender == captureButton && e.Button == MouseButtons.Left;
            FinishCapture(key, ModifierKeys);
        }
    }
}
