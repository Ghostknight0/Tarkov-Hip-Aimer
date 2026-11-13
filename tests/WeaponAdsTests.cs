// 用途：回放切枪、拔刀和投雷，验证强制关镜、右键排除及切回枪械的等待时间。
using System;
using System.Collections.Generic;

public static class WeaponAdsTests
{
    // 每一项都检查最终显隐，避免只验证内部状态而漏掉反转。
    private static int checks;

    /// <summary>复用真实输入处理入口，汇总不同武器操作的失败场景。</summary>
    public static int Run()
    {
        checks = 0;
        List<string> failures = new List<string>();
        foreach (Action scenario in new Action[] { GunSwitch, Knife, Grenade, SwitchDelay,
            SwitchEdges, GrenadeButtons, WeaponTransitions, Isolation })
        {
            try { scenario(); }
            catch (Exception error) { failures.Add(error.Message); }
        }
        if (failures.Count > 0) throw new Exception(string.Join("\n", failures.ToArray()));
        return checks;
    }

    /// <summary>数字 1/2/3 都会使当前开镜失效，切换完成后新的点击正常开镜。</summary>
    private static void GunSwitch()
    {
        foreach (int slot in new int[] { 49, 50, 51 })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Press(2);
            h.Step(0, slot);
            Visible(h, true, "切枪后立即显示瞄准点：" + slot);
            h.Step(1.5);
            h.Step(0, 2);
            Visible(h, false, "切枪结束后第一次新右键正常开镜");
        }
    }

    /// <summary>拔刀立即关镜，持刀右键不能被记为开镜，按数字键切回后恢复。</summary>
    private static void Knife()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Press(2);
        h.Step(0, 85);
        Visible(h, true, "拔刀时立即显示瞄准点");
        h.Step(10, 2);
        Visible(h, true, "持刀很久后右键仍不能记为开镜");
        h.Step(0);
        h.Step(0, 50);
        h.Step(1.5);
        h.Step(0, 2);
        Visible(h, false, "从刀切回枪，等待后恢复开镜判断");
    }

    /// <summary>拿雷和右键低抛均保持未开镜，投掷键松开后再等待一点五秒恢复。</summary>
    private static void Grenade()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Press(2);
        h.Step(0, 71);
        Visible(h, true, "拿雷时立即显示瞄准点");
        h.Step(0.1, 2);
        Visible(h, true, "拿雷后的右键低抛不能记为开镜");
        h.Step(0.1);
        h.Step(1.499, 2);
        Visible(h, true, "投掷键松开后的一点五秒内忽略开镜");
        h.Step(0.001, 2);
        Visible(h, true, "投掷等待到期时仍按住的右键不补触发");
        h.Step(0);
        h.Step(0, 2);
        Visible(h, false, "投掷等待结束后新的右键正常开镜");
    }

    /// <summary>切枪后的一点五秒内忽略点击，恰好到期时接受新的按下事件。</summary>
    private static void SwitchDelay()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Step(0, 49);
        h.Step(1.499, 2);
        Visible(h, true, "切枪一点五秒前忽略右键");
        h.Step(0.001, 2);
        Visible(h, true, "切枪到期时长按右键不补触发");
        h.Step(0);
        h.Step(0, 2);
        Visible(h, false, "切枪到期后松开再按可以开镜");

        h = new AdsActionHarness();
        h.Step(0, 50);
        h.Step(1.5, 2);
        Visible(h, false, "恰好一点五秒且是新的右键时允许开镜，长帧不延长保护");

        h = new AdsActionHarness();
        h.Step(0, 51);
        h.Step(double.NaN);
        h.Step(-1, 2);
        Visible(h, true, "无效时间不会让切枪保护提前到期");
        h.Step(1.5);
        h.Step(0, 2);
        Visible(h, false, "无效时间之后仍能正常结束切枪保护");
    }

    /// <summary>覆盖切枪长按、重复按下、按住一个再按另一个，以及右键先按住的边界。</summary>
    private static void SwitchEdges()
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Step(0, 49);
        h.Step(2, 49);
        h.Step(0, 49, 2);
        Visible(h, false, "长按同一个切枪键不会不断延长计时");

        h = new AdsActionHarness();
        h.Step(0, 49);
        h.Step(1, 49);
        h.Step(0, 49, 50);
        h.Step(1, 2);
        Visible(h, true, "按住 1 再按 2 会重新计算切枪等待");
        h.Step(0.5);
        h.Step(0, 2);
        Visible(h, false, "第二次切枪一点五秒后恢复");

        h = new AdsActionHarness();
        h.Step(0, 50);
        h.Step(1);
        h.Step(0, 50);
        h.Step(1, 2);
        Visible(h, true, "同一切枪键松开再按同样重新计时");
        h.Step(0.5);
        h.Step(0, 2);
        Visible(h, false, "重复切枪结束后新右键正常开镜");

        h = new AdsActionHarness();
        h.Step(0, 2);
        h.Step(0, 50, 2);
        Visible(h, true, "原开镜右键仍按住时切枪也立即关镜");
        h.Step(1.5, 2);
        Visible(h, true, "原开镜右键跨过整个切枪动画不会补触发");
        h.Step(0);
        h.Step(0, 2);
        Visible(h, false, "切枪后松开旧右键再按恢复");

        h = new AdsActionHarness();
        h.Step(0, 51, 2);
        Visible(h, true, "切枪与右键同帧以切枪为准");
    }

    /// <summary>投掷由拿雷后的新鼠标按压开始，左右键及同时按住均不能误记为开镜。</summary>
    private static void GrenadeButtons()
    {
        foreach (int button in new int[] { 1, 2 })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Step(0, 71);
            h.Step(10, button);
            Visible(h, true, "投掷键一直按住时不能提前恢复开镜：" + button);
            h.Step(0);
            h.Step(1.5, 2);
            Visible(h, false, "左右投掷键松开后等待一点五秒均能自动恢复");
        }
        AdsActionHarness originalAds = new AdsActionHarness();
        originalAds.Step(0, 2);
        originalAds.Step(0, 71, 2);
        originalAds.Step(0);
        originalAds.Step(5);
        originalAds.Step(0, 2);
        Visible(originalAds, true, "拿雷前旧开镜右键的松开不能误认为已经扔雷");
        originalAds.Step(0);
        originalAds.Step(1.5, 2);
        Visible(originalAds, false, "新的投掷点击松开后才开始切回等待");

        AdsActionHarness both = new AdsActionHarness();
        both.Step(0, 71);
        both.Step(0, 1, 2);
        both.Step(5, 2);
        Visible(both, true, "两个投掷键未全部松开时仍排除右键");
        both.Step(0);
        both.Step(1.5, 2);
        Visible(both, false, "投掷键全部松开后开始一点五秒等待");

        AdsActionHarness custom = new AdsActionHarness();
        custom.Config.Keys.Ads = 121;
        custom.Step(0, 71);
        custom.Step(5, 121);
        Visible(custom, true, "持雷期间自定义开镜键也不能翻转记录");
        custom.Step(0);
        custom.Step(0, 2);
        custom.Step(0);
        custom.Step(1.5, 121);
        Visible(custom, false, "投掷仍按真实鼠标键识别，不受工具开镜改键影响");
    }

    /// <summary>刀、雷和枪之间的切换互相覆盖，旧物品的状态不会拖住新操作。</summary>
    private static void WeaponTransitions()
    {
        foreach (int item in new int[] { 85, 71 })
        {
            foreach (int slot in new int[] { 49, 50, 51 })
            {
                AdsActionHarness h = new AdsActionHarness();
                h.Step(0, item);
                h.Step(0, slot);
                h.Step(1, 2);
                Visible(h, true, "从刀或雷切回任一枪槽时同样等待一点五秒");
                h.Step(0.5);
                h.Step(0, 2);
                Visible(h, false, "从刀或雷显式切回枪槽后恢复开镜");
            }
        }
        AdsActionHarness knife = new AdsActionHarness();
        knife.Step(0, 71);
        knife.Step(0, 85);
        knife.Step(0, 1);
        knife.Step(0);
        knife.Step(10, 2);
        Visible(knife, true, "拿雷后拔刀，刀的攻击松开不能被误认为扔雷结束");

        AdsActionHarness grenade = new AdsActionHarness();
        grenade.Step(0, 85);
        grenade.Step(0, 71);
        grenade.Step(0, 1);
        grenade.Step(0);
        grenade.Step(1.5, 2);
        Visible(grenade, false, "持刀后拿雷，投掷结束后自动恢复枪械判断");

        AdsActionHarness second = new AdsActionHarness();
        second.Step(0, 71);
        second.Step(0, 2);
        second.Step(0);
        second.Step(0.5, 71);
        second.Step(5, 2);
        Visible(second, true, "等待切回期间再次拿雷不能提前恢复开镜");
        second.Step(0);
        second.Step(1.5, 2);
        Visible(second, false, "第二次投雷结束后独立等待并恢复");
    }

    /// <summary>新规则不改变其他键、按住开镜、前台限制、预览、检查弹匣和其他运动状态。</summary>
    private static void Isolation()
    {
        foreach (int key in new int[] { 52, 53, 54, 97, 98, 99, 82 })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Press(2);
            h.Step(0, key);
            Visible(h, false, "快捷物品键、数字小键盘和普通换弹不当成切枪：" + key);
        }
        foreach (int key in new int[] { 49, 85, 71 })
        {
            AdsActionHarness h = new AdsActionHarness();
            h.Config.AdsMode = "hold";
            h.Step(0, key, 2);
            Visible(h, false, "按住开镜沿用原来的按键显隐");

            h = new AdsActionHarness();
            h.Config.OnlyWhenGameFocused = true;
            h.Engine.GameFocused = false;
            h.Step(0, key);
            h.Engine.GameFocused = true;
            h.Step(0, 2);
            Visible(h, false, "其他窗口的切换按键不参与跟踪");

            h = new AdsActionHarness();
            h.Preview = TarkovCrosshair.PreviewMode.Static;
            h.Step(0, key);
            h.Step(0);
            h.Preview = TarkovCrosshair.PreviewMode.Live;
            h.Step(0);
            h.Step(0, 2);
            Visible(h, false, "预览期间不开始武器切换或持刀持雷记录");

            h = new AdsActionHarness();
            h.Step(0, key);
            h.Engine.Reset();
            h.Step(0, 2);
            Visible(h, false, "重置可清除物品记录和切枪倒数");
        }
        AdsActionHarness timer = new AdsActionHarness();
        timer.Step(0, 49);
        timer.Config.OnlyWhenGameFocused = true;
        timer.Engine.GameFocused = false;
        timer.Step(2);
        timer.Engine.GameFocused = true;
        timer.Step(0, 2);
        Visible(timer, false, "切出游戏时切枪倒数仍按真实时间经过");

        AdsActionHarness magazine = new AdsActionHarness();
        magazine.Step(0, 162, 82);
        magazine.Step(0, 49);
        magazine.Step(1.5, 2);
        Visible(magazine, true, "切枪等待结束不会提前结束原有三秒检查保护");
        magazine.Step(1.5);
        magazine.Step(0, 2);
        Visible(magazine, false, "两项保护都结束后恢复开镜");

        AdsActionHarness movement = new AdsActionHarness();
        movement.Press(5); movement.Press(6); movement.Press(2);
        movement.Step(0, 50);
        Check(movement.Engine.Debug.Switched && movement.Engine.Debug.Boosted, "切枪取消开镜不重置换手和强化准星");
    }

    /// <summary>用最终透明度判断玩家看到的瞄准点状态。</summary>
    private static void Visible(AdsActionHarness h, bool expected, string message)
    {
        Check(h.Engine.Alpha == (expected ? 1 : 0), message + "，Alpha=" + h.Engine.Alpha);
    }

    /// <summary>累计具体行为检查，失败时直接说明对应玩家操作。</summary>
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
}
