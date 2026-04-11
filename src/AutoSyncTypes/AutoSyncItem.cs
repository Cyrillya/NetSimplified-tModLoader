using System;
using System.IO;
using System.Reflection;
using NetSimplified.Syncing;
using Terraria;

namespace NetSimplified.AutoSyncTypes;

internal class AutoSyncItem : AutoSyncType<Item>
{
    public AutoSyncItem() {
        CustomAttributeType = typeof(ItemSyncAttribute);
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        var syncStack = true;
        var syncFavorite = false;
        if (fieldInfo != null && Attribute.IsDefined(fieldInfo, typeof(ItemSyncAttribute))) {
            var attr = fieldInfo.GetCustomAttribute(typeof(ItemSyncAttribute)) as ItemSyncAttribute;
            if (attr != null) {
                syncStack = attr.SyncStack;
                syncFavorite = attr.SyncFavorite;
            }
        }

        bw.Write((Item) value, syncStack, syncFavorite);
    }

    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        var syncStack = true;
        var syncFavorite = false;
        if (fieldInfo != null && Attribute.IsDefined(fieldInfo, typeof(ItemSyncAttribute))) {
            var attr = fieldInfo.GetCustomAttribute(typeof(ItemSyncAttribute)) as ItemSyncAttribute;
            if (attr != null) {
                syncStack = attr.SyncStack;
                syncFavorite = attr.SyncFavorite;
            }
        }

        return r.ReadItem(syncStack, syncFavorite);
    }
}