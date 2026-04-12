using System;
using System.IO;
using System.Reflection;
using NetSimplified.Syncing;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>
///     一个灵活的 <see cref="NetModule" />，允许在不继承 <see cref="NetModule" /> 的情况下，动态定义包内容和收包行为。<br />
///     在 <see cref="Mod.Load" /> 中通过 <see cref="NetModuleLoader.Register{T}" /> 注册，随后可调用 <see cref="Set" /> 和
///     <see cref="NetModule.Send(int, int, bool)" /> 进行发包。
/// </summary>
/// <example>
///     <code>
///         // 在 Mod.Load 中注册：
///         _myModule = NetModuleLoader.Register(new FlexibleModule("MyPacket", OnReceive, new[] { typeof(int), typeof(string) }));
///         
///         // 发包：
///         _myModule.Set(new object[] { 42, "hello" });
///         _myModule.Send();
///         
///         // 收包回调：
///         void OnReceive() {
///             var number = _myModule.GetValue&lt;int&gt;(0);
///             var text   = _myModule.GetValue&lt;string&gt;(1);
///         }
///     </code>
/// </example>
public sealed class FlexibleModule : NetModule
{
    private readonly string _name;
    private readonly Action _receiveAction;
    private readonly Type[] _fieldTypes;
    private readonly MemberInfo[] _memberInfos;

    private object[] _values;

    /// <inheritdoc />
    public override string Name => $"FlexibleModule.{_name}";

    /// <summary>
    ///     创建一个 <see cref="FlexibleModule" /> 实例。
    /// </summary>
    /// <param name="name">该模块的唯一名称，用于区分不同的 <see cref="FlexibleModule" /></param>
    /// <param name="receiveAction">收包时执行的操作</param>
    /// <param name="args">
    ///     该包所包含的字段类型数组，所有类型必须已通过
    ///     <see cref="NetModuleLoader.LoadAutoSyncsFrom" /> 注册了对应的 <see cref="AutoSyncType" />
    /// </param>
    /// <param name="attributes">
    ///     可选：与 <paramref name="args" /> 一一对应的 <see cref="Attribute" /> 数组，用于控制各字段的自动传输行为
    ///     （例如 <see cref="Syncing.ItemSyncAttribute" />、<see cref="Syncing.ColorSyncAttribute" />）。
    ///     数组中的每项均可为 <see langword="null" />，表示该字段无额外属性。
    ///     若提供此参数，其长度必须与 <paramref name="args" /> 相同。
    /// </param>
    /// <exception cref="ArgumentNullException">当 <paramref name="name" /> 为 <see langword="null" /> 时抛出</exception>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="attributes" /> 不为 <see langword="null" /> 且长度与 <paramref name="args" /> 不匹配时抛出
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     当 <paramref name="args" /> 中有任何类型未注册对应的 <see cref="AutoSyncType" /> 时抛出
    /// </exception>
    public FlexibleModule(string name, Action receiveAction, Type[] args, Attribute[] attributes = null) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _receiveAction = receiveAction;
        _fieldTypes = args ?? Array.Empty<Type>();
        _values = new object[_fieldTypes.Length];

        if (attributes != null && attributes.Length != _fieldTypes.Length)
            throw new ArgumentException(
                $"attributes 长度（{attributes.Length}）与 args 长度（{_fieldTypes.Length}）不匹配");

        foreach (var type in _fieldTypes) {
            if (!AutoSyncHandler.RegisteredAutoSyncTypes.ContainsKey(type))
                throw new InvalidOperationException(
                    $"类型 {type.FullName} 未注册对应的 AutoSyncType，无法用于 FlexibleModule。" +
                    "请确保在调用 Register 前已通过 NetModuleLoader.LoadAutoSyncsFrom 加载了对应的 AutoSyncType。");
        }

        _memberInfos = new MemberInfo[_fieldTypes.Length];
        if (attributes != null) {
            for (var i = 0; i < _fieldTypes.Length; i++) {
                if (attributes[i] != null)
                    _memberInfos[i] = new AttributeMemberInfo(attributes[i]);
            }
        }
    }

    /// <summary>
    ///     对 <see cref="FlexibleModule" /> 中的变量进行赋值，赋值后才可调用
    ///     <see cref="NetModule.Send(int, int, bool)" /> 发包。
    /// </summary>
    /// <param name="args">
    ///     变量值数组，与构造时声明的变量类型一一对应。
    ///     每个元素须与对应位置的类型兼容，或为 <see langword="null" />（仅当该类型为引用类型时）。
    ///     注意：此方法执行浅拷贝，对于引用类型的元素，调用方在传入后不应再修改这些对象本身。
    /// </param>
    /// <exception cref="ArgumentException">
    ///     当 <paramref name="args" /> 的长度与字段数量不匹配，或某个值的类型与声明类型不兼容时抛出
    /// </exception>
    public void Set(object[] args) {
        if (args.Length != _fieldTypes.Length)
            throw new ArgumentException(
                $"参数数量不匹配：期望 {_fieldTypes.Length} 个，实际提供 {args.Length} 个");

        for (var i = 0; i < args.Length; i++) {
            if (args[i] != null && !_fieldTypes[i].IsInstanceOfType(args[i]))
                throw new ArgumentException(
                    $"参数 [{i}] 类型不匹配：期望 {_fieldTypes[i].FullName}，实际为 {args[i].GetType().FullName}");
        }

        _values = (object[]) args.Clone();
    }

    /// <summary>
    ///     获取接收到的第 <paramref name="index" /> 个字段的值
    /// </summary>
    /// <param name="index">字段索引（从 0 开始）</param>
    /// <returns>该字段的值</returns>
    /// <exception cref="IndexOutOfRangeException">当 <paramref name="index" /> 超出范围时抛出</exception>
    public object GetValue(int index) {
        if (index < 0 || index >= _values.Length)
            throw new IndexOutOfRangeException(
                $"索引 {index} 超出范围（共 {_values.Length} 个字段）");
        return _values[index];
    }

    /// <summary>
    ///     获取接收到的第 <paramref name="index" /> 个字段的值（泛型版本）
    /// </summary>
    /// <typeparam name="T">期望的字段类型</typeparam>
    /// <param name="index">字段索引（从 0 开始）</param>
    /// <returns>强类型的字段值</returns>
    /// <exception cref="IndexOutOfRangeException">当 <paramref name="index" /> 超出范围时抛出</exception>
    /// <exception cref="InvalidCastException">当实际类型与 <typeparamref name="T" /> 不兼容时抛出</exception>
    public T GetValue<T>(int index) => (T) GetValue(index);

    /// <inheritdoc />
    public override void Send(ModPacket p) {
        for (var i = 0; i < _fieldTypes.Length; i++)
            AutoSyncHandler.SendValue(p, _values[i], _fieldTypes[i], _memberInfos[i]);
    }

    /// <inheritdoc />
    public override void Read(BinaryReader r) {
        for (var i = 0; i < _fieldTypes.Length; i++)
            _values[i] = AutoSyncHandler.ReadValue(r, _fieldTypes[i], _memberInfos[i]);
    }

    /// <inheritdoc />
    public override void Receive() {
        _receiveAction?.Invoke();
    }

    // 合成的 MemberInfo，仅携带单个 Attribute，供 AutoSyncType 读取字段级属性。
    private sealed class AttributeMemberInfo : MemberInfo
    {
        private readonly Attribute _attribute;

        public AttributeMemberInfo(Attribute attribute) {
            _attribute = attribute;
        }

        public override MemberTypes MemberType => MemberTypes.Custom;
        public override string Name => string.Empty;
        public override Type DeclaringType => null;
        public override Type ReflectedType => null;

        public override object[] GetCustomAttributes(bool inherit) =>
            new object[] { _attribute };

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            attributeType.IsInstanceOfType(_attribute)
                ? new object[] { _attribute }
                : Array.Empty<object>();

        public override bool IsDefined(Type attributeType, bool inherit) =>
            attributeType.IsInstanceOfType(_attribute);
    }
}
