using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using NetSimplified.Syncing;
using Terraria;

namespace NetSimplified.AutoSyncTypes;

internal class AutoSyncColor : AutoSyncType<Color>
{
    public AutoSyncColor() {
        CustomAttributeType = typeof(ColorSyncAttribute);
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        var syncAlpha = true;
        if (fieldInfo != null && Attribute.IsDefined(fieldInfo, typeof(ColorSyncAttribute))) {
            var attr = fieldInfo.GetCustomAttribute(typeof(ColorSyncAttribute)) as ColorSyncAttribute;
            if (attr != null) syncAlpha = attr.SyncAlpha;
        }

        if (syncAlpha) bw.WriteRGBA((Color) value);
        else bw.WriteRGB((Color) value);
    }

    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        var syncAlpha = true;
        if (fieldInfo != null && Attribute.IsDefined(fieldInfo, typeof(ColorSyncAttribute))) {
            var attr = fieldInfo.GetCustomAttribute(typeof(ColorSyncAttribute)) as ColorSyncAttribute;
            if (attr != null) syncAlpha = attr.SyncAlpha;
        }

        return syncAlpha ? r.ReadRGBA() : r.ReadRGB();
    }
}