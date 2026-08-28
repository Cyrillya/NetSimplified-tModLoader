using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NetSimplified;
using Terraria;
using Terraria.ModLoader;

namespace NetSimplifiedExample.Common.UI;

/// <summary>
///     诊断 UI 的示例接入方式：NetSimplified 库本身不提供开关键与绘制时机，
///     由使用库的 Mod 自行处理。此处演示按 F9 / F10 切换两个 UI，并在 PostDrawInterface 中绘制。
/// </summary>
public class ExampleDiagnosticsUISystem : ModSystem
{
    private ModuleTrafficMonitor _trafficMonitor;
    private ModuleLogViewer _logViewer;

    // NetModuleLoader.PostSetupContent 创建 Diagnostics，因此这里在需要时惰性初始化
    private void EnsureCreated() {
        if (_trafficMonitor != null) return;
        if (NetModuleLoader.Diagnostics == null) return;
        _trafficMonitor = new ModuleTrafficMonitor(NetModuleLoader.Diagnostics) {
            Position = new Point(1162, 10)
        };
        _logViewer = new ModuleLogViewer(NetModuleLoader.Diagnostics) {
            Position = new Point(574, 10)
        };
    }

    public override void PostUpdateInput() {
        if (Main.dedServ) return;
        EnsureCreated();
        if (_trafficMonitor == null) return;

        if (IsPressed(Keys.F9))
            _trafficMonitor.Visible = !_trafficMonitor.Visible;

        if (IsPressed(Keys.F10))
            _logViewer.Visible = !_logViewer.Visible;
    }

    public override void PostDrawInterface(SpriteBatch spriteBatch) {
        if (Main.dedServ) return;
        EnsureCreated();
        if (_trafficMonitor == null) return;

        if (_trafficMonitor.Visible)
            _trafficMonitor.Draw(spriteBatch);

        if (_logViewer.Visible)
            _logViewer.Draw(spriteBatch);
    }

    // 边沿触发：仅在按键按下那一帧返回 true，且不干扰聊天/牌子输入
    private static bool IsPressed(Keys key) {
        return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key)
               && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest;
    }
}
