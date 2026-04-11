using System;
using System.IO;
using System.Reflection;

namespace NetSimplified;

// 用于将来自其他程序集的 AutoSyncType 实例包装为本地 AutoSyncType 以供使用的适配器
internal class ReflectiveAutoSyncAdapter : AutoSyncType
{
    private readonly object _externalInstance;
    private readonly Func<BinaryReader, MemberInfo, object> _readDelegate;
    private readonly MethodInfo _readMethodFallback;

    // 缓存，避免多次反射调用
    private readonly Action<BinaryWriter, object, MemberInfo> _sendDelegate;
    private readonly MethodInfo _sendMethodFallback;

    internal ReflectiveAutoSyncAdapter(Type associatedType, object externalInstance, MethodInfo sendMethod, MethodInfo readMethod, Type customAttributeType)
        : base(associatedType) {
        _externalInstance = externalInstance ?? throw new ArgumentNullException(nameof(externalInstance));
        _sendMethodFallback = sendMethod ?? throw new ArgumentNullException(nameof(sendMethod));
        _readMethodFallback = readMethod ?? throw new ArgumentNullException(nameof(readMethod));
        CustomAttributeType = customAttributeType;

        // 尝试创建绑定到外部实例的强类型委托以避免每次调用都使用反射
        try {
            _sendDelegate = (Action<BinaryWriter, object, MemberInfo>) Delegate.CreateDelegate(
                typeof(Action<BinaryWriter, object, MemberInfo>), _externalInstance, _sendMethodFallback);
        }
        catch {
            _sendDelegate = null;
        }

        try {
            _readDelegate = (Func<BinaryReader, MemberInfo, object>) Delegate.CreateDelegate(
                typeof(Func<BinaryReader, MemberInfo, object>), _externalInstance, _readMethodFallback);
        }
        catch {
            _readDelegate = null;
        }
    }

    public override void Send(BinaryWriter bw, object value, MemberInfo fieldInfo) {
        try {
            if (_sendDelegate != null) {
                _sendDelegate(bw, value, fieldInfo);
                return;
            }

            _sendMethodFallback.Invoke(_externalInstance, new [] { bw, value, fieldInfo });
        }
        catch (TargetInvocationException tie) {
            // unwrap
            throw tie.InnerException ?? tie;
        }
    }

    public override object Read(BinaryReader r, MemberInfo fieldInfo) {
        try {
            if (_readDelegate != null) return _readDelegate(r, fieldInfo);

            return _readMethodFallback.Invoke(_externalInstance, new object[] { r, fieldInfo });
        }
        catch (TargetInvocationException tie) {
            throw tie.InnerException ?? tie;
        }
    }
}