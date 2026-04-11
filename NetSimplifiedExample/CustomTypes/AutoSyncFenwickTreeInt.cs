using System.IO;
using System.Reflection;
using NetSimplified;
using NetSimplifiedExample.CustomTypes;

namespace NetSimplifiedExample.AutoSyncTypes;

internal class AutoSyncFenwickTreeInt : AutoSyncType<FenwickTreeInt>
{
    public override object Read(BinaryReader r, MemberInfo fieldInfo)
    {
        int len = r.ReadInt32();
        if (len < 0) return null;
        var arr = new int[len];
        for (int i = 0; i < len; i++) arr[i] = r.ReadInt32();
        return new FenwickTreeInt(arr);
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo)
    {
        var st = value as FenwickTreeInt;
        if (st == null)
        {
            bw.Write(-1);
            return;
        }

        var arr = st.ToArray();
        bw.Write(arr.Length);
        for (int i = 0; i < arr.Length; i++) bw.Write(arr[i]);
    }
}
