using NetSimplified;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace NetSimplifiedExample;

[Autoload(Side = ModSide.Client)]
public class DiagnosticsUIExampleSystem : ModSystem
{
    private FieldInfo ShouldDrawModNetDiagnosticsUIField { get; set; }
    public override void Load()
    {
        ShouldDrawModNetDiagnosticsUIField = typeof(ModNet).GetField("ShouldDrawModNetDiagnosticsUI", BindingFlags.Static | BindingFlags.NonPublic);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var index = layers.FindIndex(layer => layer.Name == $"Vanilla: Diagnose Net");
        if (index == -1) return;
        layers.Insert(index + 1, new LegacyGameInterfaceLayer($"ImproveGame: Buff Tracker GUI", () =>
        {
            // 因为ModNet.ShouldDrawModNetDiagnosticsUI是internal的所以需要反射获取了
            // 当然如果你的项目用到了公有化器也可以直接ModNet.ShouldDrawModNetDiagnosticsUI
            bool modNetShouldDraw = ShouldDrawModNetDiagnosticsUIField?.GetValue(null) is true;

            if (modNetShouldDraw)
            {
                NetModuleLoader.NetModuleDiagnosticsUI.Draw(Main.spriteBatch, 640, 110);
            }

            return true;
        }, InterfaceScaleType.UI));
    }
}
