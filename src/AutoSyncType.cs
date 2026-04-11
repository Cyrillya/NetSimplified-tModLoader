using System;
using System.IO;
using System.Reflection;

namespace NetSimplified;

/// <summary>
///     表示一个可用于在网络中自动序列化和反序列化特定类型值的抽象基类。
///     派生类需实现如何将目标类型写入包（Send）以及如何从包中读取值（Read）。
/// </summary>
public abstract class AutoSyncType
{
    /// <summary>
    ///     初始化一个新的 <see cref="AutoSyncType"/> 实例并关联指定的目标类型。
    /// </summary>
    /// <param name="type">此 AutoSyncType 关联的目标类型（例如 Color / Item / bool）。</param>
    public AutoSyncType(Type type) {
        Type = type;
    }

    /// <summary>
    ///     此 AutoSyncType 关联的类型（例如 Color / Item / bool）
    /// </summary>
    public Type Type { get; }

    /// <summary>
    ///     可选：此 AutoSyncType 对应的自定义属性类型（例如 ColorSyncAttribute / ItemSyncAttribute）
    /// </summary>
    public Type CustomAttributeType { get; protected set; }

    /// <summary>
    ///     此 AutoSyncType 应该如何将对应类型的值写入网络包。
    /// </summary>
    /// <param name="bw">用于写入值的 <see cref="BinaryWriter"/>。</param>
    /// <param name="value">要写入的数据值，通常为与此 AutoSyncType 关联的类型的实例。</param>
    /// <param name="fieldInfo">该值来源的成员信息，可为字段或属性，用于根据自定义特性或元数据决定序列化行为。</param>
    public abstract void Send(BinaryWriter bw, object value, MemberInfo fieldInfo);

    /// <summary>
    ///     此 AutoSyncType 应该如何从网络包中读取对应类型的值。
    /// </summary>
    /// <param name="r">用于读取数据的 <see cref="BinaryReader"/>。</param>
    /// <param name="fieldInfo">目标成员的成员信息，可用于基于特性或元数据调整反序列化行为。</param>
    /// <returns>返回读取并反序列化后的对象，类型应与 <see cref="Type"/> 对应。</returns>
    public abstract object Read(BinaryReader r, MemberInfo fieldInfo);
}

/// <summary>
///     提供基于泛型类型参数的 AutoSyncType 基类，方便在定义具体同步类型时使用泛型参数获得目标类型信息。
/// </summary>
public abstract class AutoSyncType<T> : AutoSyncType
{
    /// <summary>
    ///     使用泛型类型参数初始化一个新的 AutoSyncType 实例，等价于 new AutoSyncType(typeof(T))。
    /// </summary>
    public AutoSyncType() : base(typeof(T)) {
    }
}