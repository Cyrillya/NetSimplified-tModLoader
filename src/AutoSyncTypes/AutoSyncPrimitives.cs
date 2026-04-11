using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace NetSimplified.AutoSyncTypes;

internal class AutoSyncByte : AutoSyncType<byte>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadByte();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((byte) value);
    }
}

internal class AutoSyncBoolean : AutoSyncType<bool>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadBoolean();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((bool) value);
    }
}

internal class AutoSyncShort : AutoSyncType<short>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadInt16();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((short) value);
    }
}

internal class AutoSyncInt : AutoSyncType<int>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadInt32();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((int) value);
    }
}

internal class AutoSyncLong : AutoSyncType<long>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadInt64();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((long) value);
    }
}

internal class AutoSyncSByte : AutoSyncType<sbyte>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadSByte();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((sbyte) value);
    }
}

internal class AutoSyncUShort : AutoSyncType<ushort>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadUInt16();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((ushort) value);
    }
}

internal class AutoSyncUInt : AutoSyncType<uint>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadUInt32();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((uint) value);
    }
}

internal class AutoSyncULong : AutoSyncType<ulong>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadUInt64();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((ulong) value);
    }
}

internal class AutoSyncFloat : AutoSyncType<float>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadSingle();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((float) value);
    }
}

internal class AutoSyncDouble : AutoSyncType<double>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadDouble();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((double) value);
    }
}

internal class AutoSyncChar : AutoSyncType<char>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadChar();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((char) value);
    }
}

internal class AutoSyncString : AutoSyncType<string>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadString();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((string) value);
    }
}

internal class AutoSyncVector2 : AutoSyncType<Vector2>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadVector2();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.WriteVector2((Vector2) value);
    }
}

internal class AutoSyncPoint : AutoSyncType<Point>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadPoint();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((Point) value);
    }
}

internal class AutoSyncPoint16 : AutoSyncType<Point16>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        return r.ReadPoint16();
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        bw.Write((Point16) value);
    }
}

// AutoSyncItemArray 已删除，Item[] 将通过 AutoSyncItem 与通用集合/数组处理支持。

internal class AutoSyncByteArray : AutoSyncType<byte[]>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        var len = r.ReadInt32();
        return r.ReadBytes(len);
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        var buf = (byte[]) value;
        bw.Write(buf.Length);
        bw.Write(buf);
    }
}