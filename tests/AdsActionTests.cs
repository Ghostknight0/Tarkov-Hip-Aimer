// 用途：回放弹匣检查和盲射操作，验证游戏不接受的右键不会翻转瞄准点显隐。
using System;
using System.Collections.Generic;
using TarkovCrosshair;

public static class AdsActionTests
{
    // 汇总各个操作场景，失败时同时列出弹匣检查和盲射的问题。
    private static int checks;

    /// <summary>从实际按键读取到最终显隐验收，不直接改内部开镜记录。</summary>
    public static int Run()
    {
        checks = 0;
        List<string> failures = new List<string>();
        foreach (Action scenario in new Action[] { MagazineStartsHipfire, MagazineCancelsAds, MagazineTiming,
            MagazineBindings, MagazineIsolation, BlindFire, BlindFireBoundaries })
        {
            try { scenario(); }
            catch (Exception error) { failures.Add(error.Message); }
        }
        if (failures.Count > 0) throw new Exception(string.Join("\n", failures.ToArray()));
        return checks;
    }

    /// <summary>未开镜时检查弹匣，检查期间点击不会影响结束后的第一次开镜。</summary>
    private static void MagazineStartsHipfire()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Step(0.1);
        h.Step(0.1, 2);
        Visible(h, true, "弹匣检查期间的右键不应隐藏瞄准点");
        h.Step(3.0);
        h.Step(0, 2);
        Visible(h, false, "检查结束后第一次新右键应正常开镜");
    }

    /// <summary>检查前已经开镜时立即清零，检查结束后的第一次新点击正常开镜。</summary>
    private static void MagazineCancelsAds()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Press(2);
        h.Step(0, 162, 82);
        Visible(h, true, "检查触发时立即取消开镜并显示瞄准点");
        h.Step(0.1);
        h.Step(0.1, 2);
        Visible(h, true, "检查期间不能被右键重新记为开镜");
        h.Step(3.0);
        h.Step(0, 2);
        Visible(h, false, "检查结束后的新右键应正常开镜");
    }

    /// <summary>覆盖三秒边界、长帧、长按和重复检查，计时采用真实经过时间。</summary>
    private static void MagazineTiming()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Step(2.999, 2);
        Visible(h, true, "三秒前仍忽略右键");
        h.Step(0.001, 2);
        Visible(h, true, "到期时仍按住的右键不能补触发");
        h.Step(0);
        h.Step(0, 2);
        Visible(h, false, "到期后松开再按才能开镜");

        h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Step(3, 2);
        Visible(h, false, "恰好三秒且是新的右键时正常开镜，长帧不受运动限幅影响");

        h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Step(4, 162, 82);
        h.Step(0);
        h.Step(0, 2);
        Visible(h, false, "长按检查组合不会持续延长保护时间");

        h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Step(2);
        h.Step(0, 162, 82);
        h.Step(2, 2);
        Visible(h, true, "重新按下检查组合后从新的检查重新计算三秒");
        h.Step(1);
        h.Step(0, 2);
        Visible(h, false, "第二次检查计时结束可以正常开镜");

        h = new AdsActionHarness();
        h.Step(0, 162, 82, 2);
        Visible(h, true, "检查与右键同帧时以检查为准");
        h.Step(double.NaN);
        h.Step(-1, 2);
        Visible(h, true, "非法时间不会让保护提前结束");
        h.Step(3);
        h.Step(0, 2);
        Visible(h, false, "非法时间之后仍能正常到期");
    }

    /// <summary>组合键完整匹配，普通换弹、无关组合及自定义开镜键各自生效。</summary>
    private static void MagazineBindings()
    {
        foreach (int[] unrelated in new int[][] { new int[] { 82 }, new int[] { 162 },
            new int[] { 164, 82 }, new int[] { 162, 164, 82 } })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Step(0, unrelated);
            h.Step(0);
            h.Step(0, 2);
            Visible(h, false, "非配置组合不能启动检查保护");
        }
        foreach (int control in new int[] { 17, 162, 163 })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Step(0, control, 82);
            h.Step(0);
            h.Step(0, 2);
            Visible(h, true, "Ctrl 左右两侧和通用键码都能触发检查");
        }
        AdsActionHarness custom = new AdsActionHarness();
        custom.Config.Keys.MagazineCheckKey = 121;
        custom.Config.Keys.MagazineCheckModifiers = KeyChord.Alt | KeyChord.Shift;
        custom.Config.Keys.MagazineCheckBlockSeconds = 0.5;
        custom.Step(0, 164, 160, 121);
        custom.Step(0.499, 2);
        Visible(custom, true, "自定义组合和时长生效");
        custom.Step(0.001);
        custom.Step(0, 2);
        Visible(custom, false, "自定义半秒计时到期");

        custom = new AdsActionHarness();
        custom.Config.Keys.Ads = 122;
        custom.Step(0, 162, 82);
        custom.Step(0, 122);
        Visible(custom, true, "自定义开镜键在保护期间同样忽略");
        custom.Step(3);
        custom.Step(0, 122);
        Visible(custom, false, "自定义开镜键到期后恢复");

        custom = new AdsActionHarness();
        custom.Config.Keys.MagazineCheckBlockSeconds = 0;
        custom.Press(2);
        custom.Step(0, 162, 82);
        Visible(custom, false, "零秒关闭时不取消原来的开镜记录");
        custom.Step(0);
        custom.Step(0, 2);
        Visible(custom, true, "零秒关闭时也不屏蔽开镜点击");
    }

    /// <summary>计时不干扰按住开镜、其他运动、前台限制、预览和重置。</summary>
    private static void MagazineIsolation()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Config.AdsMode = "hold";
        h.Step(0, 162, 82);
        h.Step(0, 2);
        Visible(h, false, "按住开镜保持原行为");
        h.Step(0);
        Visible(h, true, "按住方式松开正常显示");

        h = new AdsActionHarness();
        h.Press(5); h.Press(6);
        h.Step(0, 162, 82);
        Check(h.Engine.Debug.Switched && h.Engine.Debug.Boosted, "检查不能重置换手和强化准星");
        h.Step(0, 9);
        h.Step(0, 2);
        Visible(h, true, "取消键不提前结束检查保护");

        h = new AdsActionHarness();
        h.Config.OnlyWhenGameFocused = true;
        h.Engine.GameFocused = false;
        h.Step(0, 162, 82);
        h.Engine.GameFocused = true;
        h.Step(0, 2);
        Visible(h, false, "其他窗口的组合不会触发检查");

        h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Config.OnlyWhenGameFocused = true;
        h.Engine.GameFocused = false;
        h.Step(4);
        h.Engine.GameFocused = true;
        h.Step(0, 2);
        Visible(h, false, "切出游戏时计时仍按真实时间结束");

        h = new AdsActionHarness();
        h.Preview = PreviewMode.Static;
        h.Step(0, 162, 82);
        h.Step(0);
        h.Preview = PreviewMode.Live;
        h.Step(0);
        h.Step(0, 2);
        Visible(h, false, "设置预览期间不启动检查保护");

        h = new AdsActionHarness();
        h.Step(0, 162, 82);
        h.Engine.Reset();
        h.Step(0, 2);
        Visible(h, false, "重置引擎时清除剩余检查时间");
    }

    /// <summary>左 Alt 配合 W 或 S 进入盲射，持续按住时不能被右键重新记为开镜。</summary>
    private static void BlindFire()
    {
        foreach (int direction in new int[] { 87, 83 })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Press(2);
            h.Step(0, 164, direction);
            Visible(h, true, "盲射应取消开镜并显示瞄准点：" + direction);
            h.Step(0.1, 164, direction, 2);
            Visible(h, true, "盲射期间的右键不应再次开镜");
            h.Step(0.1, 2);
            Visible(h, true, "松开盲射后仍按住的右键不应补触发");
            h.Step(0);
            h.Step(0, 2);
            Visible(h, false, "盲射结束后新的右键可以开镜");
        }
    }

    /// <summary>盲射使用游戏的左 Alt+W/S，不能误伤普通移动、右 Alt 或瞄具组合。</summary>
    private static void BlindFireBoundaries()
    {
        foreach (int[] unrelated in new int[][] { new int[] { 164 }, new int[] { 87 },
            new int[] { 165, 87 }, new int[] { 164, 65 }, new int[] { 162, 87 } })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Press(2);
            h.Step(0, unrelated);
            Visible(h, false, "普通移动、修饰键或侧步不能误认为盲射");
        }
        AdsActionHarness custom = new AdsActionHarness();
        custom.Config.Keys.Forward = 70;
        custom.Config.Keys.Back = 71;
        custom.Press(2);
        custom.Step(0, 164, 87, 83);
        Visible(custom, true, "相反方向过滤和工具移动改键不能漏掉真实盲射组合");

        custom = new AdsActionHarness();
        custom.Step(0, 2);
        custom.Step(0, 164, 83, 2);
        Visible(custom, true, "右键先按住再盲射也立即关镜");
        custom.Step(0, 2);
        Visible(custom, true, "盲射结束时仍按住旧右键不补触发");
        custom.Step(0);
        custom.Step(0, 2);
        Visible(custom, false, "盲射后新的开镜点击恢复");

        custom = new AdsActionHarness();
        custom.Step(0, 162, 82);
        custom.Step(3, 164, 87, 2);
        Visible(custom, true, "检查计时结束与盲射同帧时仍保持未开镜");
    }

    /// <summary>从透明度检查玩家实际看到的显隐。</summary>
    private static void Visible(AdsActionHarness h, bool expected, string message)
    {
        Check(h.Engine.Alpha == (expected ? 1 : 0), message + "，Alpha=" + h.Engine.Alpha);
    }

    /// <summary>统一累计通过的边界检查，并在失败时给出具体操作。</summary>
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
}

/// <summary>只替换系统按键查询，保留真实输入过滤、时间步长和运动引擎。</summary>
internal sealed class AdsActionHarness
{
    public readonly GlobalSettings Config = GlobalSettings.CreateDefault();
    public readonly MotionEngine Engine = new MotionEngine();
    public PreviewMode Preview = PreviewMode.Live;
    private readonly LiveInputFilter filter = new LiveInputFilter();
    private readonly HashSet<int> pressed = new HashSet<int>();

    /// <summary>去掉淡入淡出，让每次检查对应到明确的显示或隐藏。</summary>
    public AdsActionHarness()
    {
        Config.AdsMode = "toggle";
        Config.OnlyWhenGameFocused = false;
        Config.ShowFadeMs = Config.HideFadeMs = 0;
    }

    /// <summary>按照渲染线程的调用顺序推进真实经过的秒数。</summary>
    public void Step(double seconds, params int[] keys)
    {
        pressed.Clear();
        foreach (int key in keys) pressed.Add(key);
        InputState raw = FocusGate.KeysLive(Config.OnlyWhenGameFocused, Engine.GameFocused)
            ? MotionEngine.ReadInput(Config.Keys, IsDown) : new InputState();
        Engine.Update(seconds, filter.Next(raw, Config.Enabled, Preview == PreviewMode.Live),
            Config.Active, Config, Preview, 1);
    }

    /// <summary>完整点击包括松开，避免测试误造重复按下事件。</summary>
    public void Press(int key) { Step(0, key); Step(0); }

    /// <summary>通用修饰键查询同时接受左右侧，与 Windows 的查询结果保持一致。</summary>
    private bool IsDown(int key)
    {
        if (key == 16) return pressed.Contains(16) || pressed.Contains(160) || pressed.Contains(161);
        if (key == 17) return pressed.Contains(17) || pressed.Contains(162) || pressed.Contains(163);
        if (key == 18) return pressed.Contains(18) || pressed.Contains(164) || pressed.Contains(165);
        return pressed.Contains(key);
    }
}
