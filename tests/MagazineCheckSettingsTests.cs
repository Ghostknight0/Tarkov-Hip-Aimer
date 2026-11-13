// 用途：验证检查弹匣组合、时长的默认值、旧配置兼容、保存及恢复默认。
using System;
using TarkovCrosshair;

public static class MagazineCheckSettingsTests
{
    // 使用生产读写入口检查实际设置，不建立独立的配置解析规则。
    private static int checks;

    /// <summary>覆盖新安装、旧文件、非法值和设置副本，避免升级后需要重新配置。</summary>
    public static int Run()
    {
        checks = 0;
        AssertBinding(new GlobalSettings(), 82, 1, 3, "新配置默认 Ctrl+R 和三秒");
        foreach (string json in new string[] { "{}", "{\"Keys\":null}", "{\"Keys\":{}}" })
            AssertBinding(Parse(json), 82, 1, 3, "旧配置补默认检查保护");
        GlobalSettings custom = new GlobalSettings();
        custom.Keys.MagazineCheckKey = 121;
        custom.Keys.MagazineCheckModifiers = KeyChord.Alt | KeyChord.Shift;
        custom.Keys.MagazineCheckBlockSeconds = 4.5;
        AssertBinding(Parse(SettingsIO.Serialize(custom)), 121, 6, 4.5, "自定义组合和时长保存读取");
        GlobalSettings copy = custom.Clone(false);
        copy.Keys.MagazineCheckKey = 120;
        copy.Keys.MagazineCheckModifiers = 0;
        copy.Keys.MagazineCheckBlockSeconds = 1;
        AssertBinding(custom, 121, 6, 4.5, "发布副本不改变 UI 正在编辑的值");
        custom.ResetKeys();
        AssertBinding(custom, 82, 1, 3, "按键页恢复默认同时恢复检查保护");
        AssertBinding(Parse("{\"Keys\":{\"MagazineCheckBlockSeconds\":0}}"), 82, 1, 0, "显式零秒关闭不会被补回三秒");
        AssertBinding(Parse("{\"Keys\":{\"MagazineCheckKey\":0,\"MagazineCheckModifiers\":8,\"MagazineCheckBlockSeconds\":-1}}"),
            82, 1, 0, "非法键和修饰键补默认，负时长限制为零");
        AssertBinding(Parse("{\"Keys\":{\"MagazineCheckKey\":17,\"MagazineCheckBlockSeconds\":100}}"),
            82, 1, 30, "修饰键不能作为主键，时长不能超过 UI 上限");
        custom.Keys.MagazineCheckBlockSeconds = double.NaN;
        custom.Validate();
        AssertBinding(custom, 82, 1, 3, "无效数值恢复默认时间");
        return checks;
    }

    /// <summary>调用项目自己的解析和校验，错误时显示原始错误说明。</summary>
    private static GlobalSettings Parse(string json)
    {
        GlobalSettings settings;
        string error;
        if (!SettingsIO.TryParse(json, out settings, out error)) throw new Exception(error);
        return settings;
    }

    /// <summary>同时核对主键、修饰键和保护秒数，防止只保存了其中一部分。</summary>
    private static void AssertBinding(GlobalSettings settings, int key, int modifiers, double seconds, string message)
    {
        checks++;
        KeyBindings actual = settings.Keys;
        if (actual.MagazineCheckKey != key || actual.MagazineCheckModifiers != modifiers || actual.MagazineCheckBlockSeconds != seconds)
            throw new Exception(message);
    }
}
