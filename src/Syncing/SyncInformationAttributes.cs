using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace NetSimplified.Syncing;

/// <summary>
///     用于决定是否传输 <see cref="Item" /> 的 <see cref="Item.stack" /> 与 <see cref="Item.favorited" /> 信息的特性
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ItemSyncAttribute : Attribute
{
    internal readonly bool SyncFavorite;
    internal readonly bool SyncStack;

    /// <summary>
    ///     <inheritdoc cref="ItemSyncAttribute" />
    /// </summary>
    /// <param name="syncStack">是否传输物品的 <see cref="Item.stack" /> 信息</param>
    /// <param name="syncFavorite">是否传输物品的 <see cref="Item.favorited" /> 信息</param>
    public ItemSyncAttribute(bool syncStack = true, bool syncFavorite = false) {
        SyncStack = syncStack;
        SyncFavorite = syncFavorite;
    }
}

/// <summary>
///     用于决定是否传输 <see cref="Color" /> 的 <see cref="Color.A" /> 信息的特性
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ColorSyncAttribute : Attribute
{
    internal readonly bool SyncAlpha;

    /// <summary>
    ///     <inheritdoc cref="ColorSyncAttribute" />
    /// </summary>
    /// <param name="syncAlpha">是否传输 <see cref="Color" /> 的 <see cref="Color.A" /> 信息</param>
    public ColorSyncAttribute(bool syncAlpha = false) {
        SyncAlpha = syncAlpha;
    }
}