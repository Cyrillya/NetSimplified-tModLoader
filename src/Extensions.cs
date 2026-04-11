using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;

namespace NetSimplified;

/// <summary>
///     发包的一些实用方法
/// </summary>
public static class Extensions
{
    /// <summary>
    ///     写入 <see cref="Color" /> RGBA值<br />
    ///     对应: <see cref="ReadRGBA(BinaryReader)" />
    /// </summary>
    public static void WriteRGBA(this BinaryWriter w, Color color) {
        w.Write(color.R);
        w.Write(color.G);
        w.Write(color.B);
        w.Write(color.A);
    }

    /// <summary>
    ///     读取 <see cref="Color" /> RGBA值<br />
    ///     对应: <see cref="WriteRGBA(BinaryWriter, Color)" />
    /// </summary>
    public static Color ReadRGBA(this BinaryReader r) {
        return new Color(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte());
    }

    /// <summary>
    ///     写入 <see cref="Point" /> 坐标<br />
    ///     对应: <see cref="ReadPoint(BinaryReader)" />
    /// </summary>
    public static void Write(this BinaryWriter w, Point point) {
        w.Write(point.X);
        w.Write(point.Y);
    }

    /// <summary>
    ///     读取 <see cref="Point" /> 坐标<br />
    ///     对应: <see cref="Write(BinaryWriter, Point)" />
    /// </summary>
    public static Point ReadPoint(this BinaryReader r) {
        return new Point(r.ReadInt32(), r.ReadInt32());
    }

    /// <summary>
    ///     写入 <see cref="Point16" /> 坐标<br />
    ///     对应: <see cref="ReadPoint16(BinaryReader)" />
    /// </summary>
    public static void Write(this BinaryWriter w, Point16 point) {
        w.Write(point.X);
        w.Write(point.Y);
    }

    /// <summary>
    ///     读取 <see cref="Point16" /> 坐标<br />
    ///     对应: <see cref="Write(BinaryWriter, Point16)" />
    /// </summary>
    public static Point16 ReadPoint16(this BinaryReader r) {
        return new Point16(r.ReadInt16(), r.ReadInt16());
    }

    /// <summary>
    ///     写入 <see cref="Item" /> 若值为 <see langword="null" /> 则直接实例化一个空 <see cref="Item" /><br />
    ///     对应: <see cref="ReadItem(BinaryReader, bool, bool)" />
    /// </summary>
    public static void Write(this BinaryWriter w, Item item, bool writeStack = true, bool writeFavorite = false) {
        ItemIO.Send(item ?? new Item(), w, writeStack, writeFavorite);
    }

    /// <summary>
    ///     读取 <see cref="Item" /><br />
    ///     对应: <see cref="Write(BinaryWriter, Item, bool, bool)" />
    /// </summary>
    public static Item ReadItem(this BinaryReader r, bool readStack = true, bool readFavorite = false) {
        return ItemIO.Receive(r, readStack, readFavorite);
    }

    /// <summary>
    ///     写入 <see cref="Item" /> 数组, 若存在值为 <see langword="null" /> 的物品则直接实例化一个空 <see cref="Item" /><br />
    ///     对应: <see cref="ReadItemArray(BinaryReader, bool, bool)" />
    /// </summary>
    public static void Write(this BinaryWriter w, Item[] items, bool writeStack = true, bool writeFavorite = false) {
        w.Write(items.Length);
        for (var i = 0; i < items.Length; i++) ItemIO.Send(items[i] ?? new Item(), w, writeStack, writeFavorite);
    }

    /// <summary>
    ///     读取 <see cref="Item" /> 数组<br />
    ///     对应: <see cref="Write(BinaryWriter, Item[], bool, bool)" />
    /// </summary>
    public static Item[] ReadItemArray(this BinaryReader r, bool readStack = true, bool readFavorite = false) {
        var length = r.ReadInt32();
        var items = new Item[length];
        for (var i = 0; i < length; i++) items[i] = ItemIO.Receive(r, readStack, readFavorite);
        return items;
    }
}