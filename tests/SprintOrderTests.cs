// 用途：验证 Shift 和前进键的先后顺序，避免先屏息再前进被误认为奔跑关镜。
using System;

public static class SprintOrderTests
{
    // 使用真实输入读取和显隐输出，不模拟游戏内部的开镜状态。
    private static int checks;

    /// <summary>覆盖通用、左右 Shift，前进顺序和相反方向切换。</summary>
    public static int Run()
    {
        checks = 0;
        foreach (int shift in new int[] { 16, 160, 161 })
        {
            ShiftBeforeForward(shift);
            ForwardBeforeShift(shift);
            OppositeDirection(shift);
        }
        return checks;
    }

    /// <summary>Shift 先按住时，前进、停步和再次前进都不能清除开镜记录。</summary>
    private static void ShiftBeforeForward(int shift)
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Press(2);
        h.Step(0, shift);
        h.Step(0, shift, 87);
        Visible(h, false, "先 Shift 后 W 保留开镜：" + shift);
        h.Step(0, shift);
        h.Step(0, shift, 87);
        Visible(h, false, "Shift 不松开，停止再前进仍不取消开镜");
        h.Step(0, shift, 87, 2);
        Visible(h, true, "先屏息再前进时右键仍可以正常关镜");
        h.Step(0, shift, 87);
        h.Step(0, shift, 87, 2);
        Visible(h, false, "先屏息再前进时新的右键仍可以正常开镜");
    }

    /// <summary>已经有效前进时，新按下 Shift 才取消开镜，持续按住不反复清零。</summary>
    private static void ForwardBeforeShift(int shift)
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Press(2);
        h.Step(0, 87);
        h.Step(0, 87, shift);
        Visible(h, true, "先 W 后 Shift 仍取消开镜：" + shift);
        h.Step(0, 87, shift, 2);
        Visible(h, false, "奔跑组合保持时新的右键允许开镜");
        h.Step(0, 87, shift, 2);
        Visible(h, false, "Shift 长按不能反复清除新开镜记录");
        h.Step(0, 87, 2);
        h.Step(0, 87, shift, 2);
        Visible(h, true, "前进时松开再按 Shift 再次取消开镜");
    }

    /// <summary>按 Shift 时如果仍在后退，之后仅松开后退不会产生新的 Shift 按下。</summary>
    private static void OppositeDirection(int shift)
    {
        AdsActionHarness h = new AdsActionHarness();
        h.Press(2);
        h.Step(0, 87);
        h.Step(0, 87, 83, shift);
        Visible(h, false, "有效方向为后退时按 Shift 不取消开镜");
        h.Step(0, 87, shift);
        Visible(h, false, "Shift 已按住，恢复前进时不取消开镜");
        h.Step(0, 87);
        h.Step(0, 87, shift);
        Visible(h, true, "恢复前进后重新按 Shift 才取消开镜");
    }

    /// <summary>以瞄准点的最终透明度验收用户看到的显示结果。</summary>
    private static void Visible(AdsActionHarness h, bool expected, string message)
    {
        checks++;
        if (h.Engine.Alpha != (expected ? 1 : 0)) throw new Exception(message + "，Alpha=" + h.Engine.Alpha);
    }
}
