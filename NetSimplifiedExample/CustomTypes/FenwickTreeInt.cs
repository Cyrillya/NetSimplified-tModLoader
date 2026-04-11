// filepath: f:\Terraria\Github\NetSimplified\NetSimplifiedExample\CustomTypes\SegmentTreeIntSum.cs
using System;

namespace NetSimplifiedExample.CustomTypes;

/// <summary>
/// 基于差分的区间加法 + 区间求和的数据结构（Fenwick 树实现）。
/// 原名 SegmentTreeIntSum，已重命名为 FenwickTreeInt，不再继承自 SegmentTree&lt;int&gt;。
/// 支持 RangeAdd 和 RangeSum 操作。
/// </summary>
public class FenwickTreeInt
{
    private readonly int _n;
    private readonly long[] _bit1;
    private readonly long[] _bit2;

    public FenwickTreeInt(int n)
    {
        _n = n;
        _bit1 = new long[_n + 2];
        _bit2 = new long[_n + 2];
    }

    public FenwickTreeInt(int[] initial)
    {
        _n = initial?.Length ?? 0;
        _bit1 = new long[_n + 2];
        _bit2 = new long[_n + 2];
        if (_n > 0 && initial != null)
        {
            // 构建为每个位置添加初始值
            for (int i = 0; i < _n; i++)
            {
                RangeAdd(i, i, initial[i]);
            }
        }
    }

    private void AddInternal(long[] bit, int idx, long val)
    {
        idx++; // 使用 1-based 内部索引
        while (idx <= _n + 1)
        {
            bit[idx] += val;
            idx += idx & -idx;
        }
    }

    private long SumInternal(long[] bit, int idx)
    {
        idx++; // 1-based
        long res = 0;
        while (idx > 0)
        {
            res += bit[idx];
            idx -= idx & -idx;
        }
        return res;
    }

    // 在区间 [l, r] 上加上 val
    public void RangeAdd(int l, int r, int val)
    {
        if (l < 0 || r < l || r >= _n) throw new ArgumentOutOfRangeException();
        long v = val;
        // 对 bit1 和 bit2 做差分
        AddInternal(_bit1, l, v);
        AddInternal(_bit1, r + 1, -v);
        AddInternal(_bit2, l, v * (l - 1));
        AddInternal(_bit2, r + 1, -v * r);
    }

    // 前缀和 [0..idx]
    private long PrefixSum(int idx)
    {
        if (idx < 0) return 0;
        if (idx >= _n) idx = _n - 1;
        long s1 = SumInternal(_bit1, idx);
        long s2 = SumInternal(_bit2, idx);
        return s1 * idx - s2;
    }

    // 区间求和 [l, r]
    public long RangeSum(int l, int r)
    {
        if (l < 0 || r < l || r >= _n) throw new ArgumentOutOfRangeException();
        return PrefixSum(r) - PrefixSum(l - 1);
    }

    // 获取指定位置（点）的值
    public int GetPoint(int idx)
    {
        if (idx < 0 || idx >= _n) throw new ArgumentOutOfRangeException(nameof(idx));
        return (int)RangeSum(idx, idx);
    }

    // 导出当前数组快照（用于序列化）
    public int[] ToArray()
    {
        var arr = new int[_n];
        for (int i = 0; i < _n; i++) arr[i] = GetPoint(i);
        return arr;
    }
}
