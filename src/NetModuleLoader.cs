using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetSimplified.Syncing;
using Terraria;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>
///     用于加载 <see cref="NetModule" /> 的类。
/// </summary>
public class NetModuleLoader : ModSystem
{
    internal static Dictionary<string, FieldInfo[]> FieldInfos = new();
    private static List<NetModule> _modules = new();

    /// <summary>
    ///     当前模组实例，必须在调用 <see cref="LoadNetModulesFrom" /> 前设置，以确保 NetModule 能正确设置 Mod 字段。建议在 <see cref="Mod.Load" /> 中设置。之后可通过 <see cref="NetModule.Mod" /> 访问
    /// </summary>
    public static Mod CurrentMod;

    /// <summary>
    ///     用于记录各 NetModule 的传输量
    /// </summary>
    public static NetModuleDiagnostics NetModuleDiagnosticsUI { get; private set; }

    /// <inheritdoc />
    public override void PostSetupContent() {
        NetModuleDiagnosticsUI = Main.dedServ ? null : new NetModuleDiagnostics(_modules);
    }

    /// <inheritdoc />
    public override void PreSaveAndQuit() {
        NetModuleDiagnosticsUI.Reset();
    }

    // Helper: 判断 type 是否从一个全名为 baseFullName 的基类或接口派生（跨程序集场景）
    private static bool IsDerivedByName(Type type, string baseFullName) {
        if (type == null || baseFullName == null) return false;
        // 检查自身
        if (type.FullName == baseFullName) return true;
        // 检查基类链
        var bt = type.BaseType;
        while (bt != null) {
            if (bt.FullName == baseFullName) return true;
            bt = bt.BaseType;
        }

        // 检查接口
        foreach (var iface in type.GetInterfaces())
            if (iface.FullName == baseFullName)
                return true;
        return false;
    }


    /// <summary>
    ///     从指定程序集加载并注册所有继承自 AutoSyncType 的类型<br/>
    ///     模组应在调用 Register 或 LoadNetModulesFrom 前先调用此方法以确保 AutoSyncType 已注册。<br/>
    ///     在调用前，请先设置 <see cref="Mod" /> 字段，以确保 NetModule 能正确设置 Mod 字段。建议在 <see cref="Mod.Load" /> 中设置。之后可通过 <see cref="NetModule.Mod" /> 访问。
    /// </summary>
    public static void LoadAutoSyncsFrom(Assembly asm) {
        if (asm == null) return;
        try {
            var baseType = typeof(AutoSyncType);
            var baseFullName = baseType.FullName;
            foreach (var type in asm.GetTypes()) {
                if (type.IsAbstract) continue;
                // 支持跨程序集但同名的派生类型
                if (!(baseType.IsAssignableFrom(type) || IsDerivedByName(type, baseFullName))) continue;
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null) continue;
                try {
                    var obj = Activator.CreateInstance(type)!;

                    // 如果可以直接 cast，则使用当前模组的 handler 注册
                    if (obj is AutoSyncType localInstance) {
                        AutoSyncHandler.RegisterType(localInstance);
                        CurrentMod?.Logger.Info($"已为类型 {localInstance.Type} 绑定 AutoSyncType: {type.Name}");
                        continue;
                    }

                    // 反射适配：尝试读取外部实例的相关成员并包装为本地 AutoSyncType
                    var propType = type.GetProperty("Type", BindingFlags.Public | BindingFlags.Instance);
                    var propCustom = type.GetProperty("CustomAttributeType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    var sendMethod = type.GetMethod("Send", BindingFlags.Public | BindingFlags.Instance);
                    var readMethod = type.GetMethod("Read", BindingFlags.Public | BindingFlags.Instance);

                    if (propType == null || sendMethod == null || readMethod == null) continue;

                    var associatedType = propType.GetValue(obj) as Type;
                    var customAttrType = propCustom?.GetValue(obj) as Type;

                    if (associatedType == null) continue;

                    var adapter = new ReflectiveAutoSyncAdapter(associatedType, obj, sendMethod, readMethod, customAttrType);
                    AutoSyncHandler.RegisterType(adapter);
                    CurrentMod?.Logger.Info($"已为类型 {adapter.Type} 绑定外部 AutoSyncType 适配器: {type.Name}");
                }
                catch {
                    // 忽略无法实例化或适配的 AutoSyncType
                }
            }
        }
        catch {
            // 忽略反射错误
        }
    }

    /// <summary>
    ///     加载并注册所有继承自 NetModule 的类型。 <br/>
    ///     在调用前，请先确保所有所需的 AutoSyncType 已通过 <see cref="LoadAutoSyncsFrom" /> 注册，以免出现 AutoSyncType 未注册导致的错误。<br/>
    ///     在调用前，请先设置 <see cref="Mod" /> 字段，以确保 NetModule 能正确设置 Mod 字段。建议在 <see cref="Mod.Load" /> 中设置。
    /// </summary>
    public static void LoadNetModules() {
        LoadNetModulesFrom(typeof(NetModuleLoader).Assembly);
        LoadNetModulesFrom(Assembly.GetCallingAssembly());
    }

    /// <summary>
    ///     从指定程序集加载并注册所有继承自 NetModule 的类型。<br/>
    ///     原本用于支持加载其他模组的 NetModule，但实际上无法实现，因此现在不需要调用此方法，直接调用 LoadNetModules() 即可。
    /// </summary>
    public static void LoadNetModulesFrom(Assembly asm) {
        if (asm == null) return;
        try {
            var baseType = typeof(NetModule);
            foreach (var type in asm.GetTypes()) {
                if (type.IsAbstract) continue;
                if (!baseType.IsAssignableFrom(type)) continue;
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null) continue;
                try {
                    var instance = (NetModule) Activator.CreateInstance(type)!;
                    Register(instance);
                    CurrentMod?.Logger.Info("已加载 NetModule: " + instance.Name);
                }
                catch {
                    // 忽略无法实例化的 NetModule
                }
            }
        }
        catch {
            // 忽略反射错误
        }
    }

    public static void Register(NetModule netModule) {
        _modules ??= new List<NetModule>();
        FieldInfos ??= new Dictionary<string, FieldInfo[]>();

        if (_modules.Contains(netModule)) throw new Exception("不能重复注册! " + netModule.Name);

        netModule.Type = _modules.Count;
        netModule.Mod = CurrentMod;
        _modules.Add(netModule);

        // 设立 fieldInfo 列表
        var fieldsIncluded = netModule.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        var isClassAutoSync = Attribute.IsDefined(netModule.GetType(), typeof(AutoSyncAttribute));

        bool IsTypeSupported(Type t) {
            // 已注册的 AutoSyncType（闭合类型）
            if (AutoSyncHandler.RegisteredAutoSyncTypes.ContainsKey(t)) return true;

            // 数组（支持多维/嵌套）
            if (t.IsArray) {
                var elem = t.GetElementType();
                return elem != null && IsTypeSupported(elem);
            }

            // KeyValuePair<TKey, TValue>
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
                var args = t.GetGenericArguments();
                return IsTypeSupported(args[0]) && IsTypeSupported(args[1]);
            }

            // IEnumerable<T>
            var enumInterface = t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumInterface != null) {
                var elem = enumInterface.GetGenericArguments()[0];
                return IsTypeSupported(elem);
            }

            return false;
        }

        // 只有类或变量被标记AutoSync且是支持的变量类型才能纳入
        var fields = fieldsIncluded.Where(fieldInfo =>
            (isClassAutoSync || Attribute.IsDefined(fieldInfo, typeof(AutoSyncAttribute))) &&
            IsTypeSupported(fieldInfo.FieldType));
        var fieldInfos = fields as FieldInfo[] ?? fields.ToArray();
        if (fieldInfos.Any()) FieldInfos[netModule.Name] = fieldInfos.ToArray();
    }

    /// <summary>
    ///     根据 <paramref name="type" /> 获取相应的 <see cref="NetModule" /> 实例
    /// </summary>
    /// <returns><see cref="NetModule" /> 实例</returns>
    public static NetModule Get(int type) {
        return _modules[type];
    }

    /// <summary>
    ///     根据 <paramref name="name" /> 获取相应的 <see cref="NetModule" /> 实例
    /// </summary>
    /// <returns><see cref="NetModule" /> 实例</returns>
    public static NetModule Get(string name) {
        return _modules.First(m => m.Name == name);
    }

    /// <summary>
    ///     获取带有Mod与Type信息的 <see cref="NetModule" /> 实例
    /// </summary>
    /// <returns><see cref="NetModule" /> 实例</returns>
    public static T Get<T>() where T : NetModule {
        return (T) _modules.FirstOrDefault(m => m is T || m.GetType() == typeof(T));
    }
}