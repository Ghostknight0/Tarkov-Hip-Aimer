// 用途：处理默认切枪、拔刀和拿雷操作，避免切换期间或持刀持雷时的右键被记为开镜。
using System;

namespace TarkovCrosshair
{
    public sealed partial class MotionEngine
    {
        // 只按玩家操作推测手持物，不读取游戏进程；显式切回枪械时重新对齐。
        private enum HeldWeapon { Gun, Knife, Grenade }
        private HeldWeapon heldWeapon;
        private byte prevWeaponSlots, prevThrowButtons;
        private bool prevDrawKnife, prevDrawGrenade, grenadeThrowStarted;
        private double weaponSwitchRemaining;
        // 切枪和投雷后统一等待一点五秒；按住旧右键不会在到期时补触发。
        private const double WeaponSwitchSeconds = 1.5;

        /// <summary>单独记录各个数字键，按住 1 再按 2 也能识别新的切枪。</summary>
        private static void ReadWeaponInput(ref InputState input, KeyBindings keys, Func<int, bool> isDown)
        {
            if (isDown(0x31)) input.WeaponSlots |= 1;
            if (isDown(0x32)) input.WeaponSlots |= 2;
            if (isDown(0x33)) input.WeaponSlots |= 4;
            input.DrawKnife = isDown(0x55);
            input.DrawGrenade = isDown(0x47);
            // 默认绑定复用本帧已读结果；改键时另查真实鼠标，投掷不会随开镜键一起改变。
            bool leftThrow = keys.Fire == 0x01 ? input.Fire : isDown(0x01);
            bool rightThrow = keys.Ads == 0x02 ? input.Ads : isDown(0x02);
            if (leftThrow) input.ThrowButtons |= 1;
            if (rightThrow) input.ThrowButtons |= 2;
        }

        /// <summary>重播或重置时清除物品记录和倒数，回到可正常判断开镜的初始状态。</summary>
        private void ResetWeaponInput()
        {
            heldWeapon = HeldWeapon.Gun;
            prevWeaponSlots = prevThrowButtons = 0;
            prevDrawKnife = prevDrawGrenade = grenadeThrowStarted = false;
            weaponSwitchRemaining = 0;
        }

        /// <summary>按真实经过时间更新切换保护，预览期间只接续真实按键历史。</summary>
        private void UpdateWeaponInput(InputState input, GlobalSettings settings, bool preview, double elapsed)
        {
            if (!double.IsNaN(elapsed) && elapsed > 0)
                weaponSwitchRemaining = Math.Max(0, weaponSwitchRemaining - elapsed);
            if (weaponSwitchRemaining < 1e-9) weaponSwitchRemaining = 0;
            if (settings.AdsMode != "toggle")
            {
                // 按住开镜沿用原规则，不保留点切模式下的物品和倒数记录。
                heldWeapon = HeldWeapon.Gun;
                weaponSwitchRemaining = 0;
                grenadeThrowStarted = false;
            }
            else if (!preview)
            {
                bool gunPressed = (input.WeaponSlots & ~prevWeaponSlots) != 0;
                if (input.DrawGrenade && !prevDrawGrenade)
                    SelectWeapon(HeldWeapon.Grenade);
                else if (input.DrawKnife && !prevDrawKnife)
                    SelectWeapon(HeldWeapon.Knife);
                else if (gunPressed)
                    SelectWeapon(HeldWeapon.Gun);
                UpdateGrenadeThrow(input);
            }
            prevWeaponSlots = input.WeaponSlots;
            prevDrawKnife = input.DrawKnife;
            prevDrawGrenade = input.DrawGrenade;
            prevThrowButtons = input.ThrowButtons;
        }

        /// <summary>每次新切换都取消旧物品的投掷记录，切回枪械时重新等待一点五秒。</summary>
        private void SelectWeapon(HeldWeapon weapon)
        {
            heldWeapon = weapon;
            grenadeThrowStarted = false;
            weaponSwitchRemaining = weapon == HeldWeapon.Gun ? WeaponSwitchSeconds : 0;
        }

        /// <summary>拿雷之后新的左右键才算投掷；旧开镜右键松开不会被误认为已经扔雷。</summary>
        private void UpdateGrenadeThrow(InputState input)
        {
            if (heldWeapon != HeldWeapon.Grenade) return;
            if ((input.ThrowButtons & ~prevThrowButtons) != 0) grenadeThrowStarted = true;
            if (grenadeThrowStarted && input.ThrowButtons == 0)
                SelectWeapon(HeldWeapon.Gun);
        }

        /// <summary>持刀、持雷或枪械尚未切换完成时，开镜点击都不能翻转记录。</summary>
        private bool WeaponBlocksAds
        {
            get { return heldWeapon != HeldWeapon.Gun || weaponSwitchRemaining > 0; }
        }
    }
}
