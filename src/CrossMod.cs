using System;
using Terraria.ModLoader;

namespace NetSimplified;

/// <summary>
///     提供与其他 tModLoader 模组之间的调用与模块传递的辅助方法。
///     包含用于获取外部模块实例、发送模块实例到其它客户端以及在跨模组调用中处理操作的通用入口。
/// </summary>
public static class CrossMod
{
    /// <summary>
    ///     处理来自其他模组的通用调用接口。根据第一个字符串参数决定具体操作（例如获取或发送模块）。
    /// </summary>
    /// <param name="args">来自模组调用的参数数组，第一个参数应为操作名称字符串。</param>
    /// <returns>对调用的处理结果，成功与否或请求的数据（取决于操作）。返回 false 代表并没有在调用 NetSimplified 的接口，返回 null 表示调用不成功，其他返回表示成功调用，返回了对应的处理结果。</returns>
    public static object HandleModCalls(params object[] args) {
        if (args == null || args.Length == 0) return false;
        if (args[0] is not string operation) return false;

        if (operation == "NetSimplified_GetModule") {
            if (args.Length < 2) return null;
            // 支持按名称获取模块实例
            if (args[1] is string name)
                try {
                    return NetModuleLoader.Get(name);
                }
                catch {
                    return null;
                }

            return null;
        }

        if (operation == "NetSimplified_SendModule") {
            if (args.Length < 2) return null;

            // args[1] 必然是之前通过 NetSimplified_GetModule 获取到的 NetModule 实例，直接使用并调用其 Send
            if (args[1] is not NetModule passedModule) return null;

            var toClient = -1;
            var ignoreClient = -1;
            var runLocally = false;

            if (args.Length > 2 && args[2] is int) toClient = (int) args[2];
            if (args.Length > 3 && args[3] is int) ignoreClient = (int) args[3];
            if (args.Length > 4 && args[4] != null) runLocally = Convert.ToBoolean(args[4]);

            // 直接对传入的实例调用 Send（传入的实例可能在外部被修改过）
            passedModule.Send(toClient, ignoreClient, runLocally);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     尝试从已加载的模组中按名称获取外部模块实例（返回 object）。
    /// </summary>
    /// <param name="mod">模组的名称。</param>
    /// <param name="name">模块的名称。</param>
    /// <param name="module">若成功则输出模块实例，否则为 null。</param>
    /// <returns>如果成功获取则为 true，否则为 false。</returns>
    public static bool TryGetExternalModule(string mod, string name, out object module) {
        module = null;
        return ModLoader.TryGetMod(mod, out var modInstance) && TryGetExternalModule(modInstance, name, out module);
    }

    /// <summary>
    ///     尝试从已加载的模组中按类型获取外部模块实例（泛型版本）。
    /// </summary>
    /// <typeparam name="T">期望的模块类型。</typeparam>
    /// <param name="mod">模组的名称。</param>
    /// <param name="module">若成功则输出模块实例（T 类型），否则为 null。</param>
    /// <returns>如果成功获取则为 true，否则为 false。</returns>
    public static bool TryGetExternalModule<T>(string mod, out T module) where T : class {
        module = null;
        return ModLoader.TryGetMod(mod, out var modInstance) && TryGetExternalModule(modInstance, out module);
    }

    /// <summary>
    ///     尝试从指定的 Mod 实例中按名称获取外部模块实例（返回 object）。
    /// </summary>
    /// <param name="modInstance">目标模组的 Mod 实例。</param>
    /// <param name="name">模块的名称。</param>
    /// <param name="module">若成功则输出模块实例，否则为 null。</param>
    /// <returns>如果成功获取则为 true，否则为 false。</returns>
    public static bool TryGetExternalModule(Mod modInstance, string name, out object module) {
        module = null;
        var reply = modInstance.Call("NetSimplified_GetModule", name);
        if (reply is null) return false;
        module = reply;
        return true;
    }

    /// <summary>
    ///     尝试从指定的 Mod 实例中按类型获取外部模块实例（泛型版本）。
    /// </summary>
    /// <typeparam name="T">期望的模块类型。</typeparam>
    /// <param name="modInstance">目标模组的 Mod 实例。</param>
    /// <param name="module">若成功则输出模块实例（T 类型），否则为 null。</param>
    /// <returns>如果成功获取则为 true，否则为 false。</returns>
    public static bool TryGetExternalModule<T>(Mod modInstance, out T module) where T : class {
        module = null;
        var result = TryGetExternalModule(modInstance, typeof(T).Name, out var reply);
        if (reply is not T typedReply) return false;
        module = typedReply;
        return result;
    }

    /// <summary>
    ///     从指定模组名称获取外部模块实例（非泛型，若失败返回 null）。
    /// </summary>
    /// <param name="mod">模组名称。</param>
    /// <param name="name">模块名称。</param>
    /// <returns>找到的模块实例，或 null。</returns>
    public static object GetExternalModule(string mod, string name) {
        TryGetExternalModule(mod, name, out var module);
        return module;
    }

    /// <summary>
    ///     从指定 Mod 实例获取外部模块实例（非泛型，若失败返回 null）。
    /// </summary>
    /// <param name="modInstance">目标 Mod 实例。</param>
    /// <param name="name">模块名称。</param>
    /// <returns>找到的模块实例，或 null。</returns>
    public static object GetExternalModule(Mod modInstance, string name) {
        TryGetExternalModule(modInstance, name, out var module);
        return module;
    }

    /// <summary>
    ///     从指定模组名称获取外部模块实例（泛型版本，若失败返回 null）。
    /// </summary>
    /// <typeparam name="T">期望的模块类型。</typeparam>
    /// <param name="mod">模组名称。</param>
    /// <returns>找到的模块实例（T 类型），或 null。</returns>
    public static T GetExternalModule<T>(string mod) where T : class {
        TryGetExternalModule<T>(mod, out var module);
        return module;
    }

    /// <summary>
    ///     从指定 Mod 实例获取外部模块实例（泛型版本，若失败返回 null）。
    /// </summary>
    /// <typeparam name="T">期望的模块类型。</typeparam>
    /// <param name="modInstance">目标 Mod 实例。</param>
    /// <returns>找到的模块实例（T 类型），或 null。</returns>
    public static T GetExternalModule<T>(Mod modInstance) where T : class {
        TryGetExternalModule<T>(modInstance, out var module);
        return module;
    }

    /// <summary>
    ///     尝试将给定模块实例通过目标 Mod 进行发送（用于跨模组的模块传递）。
    /// </summary>
    /// <param name="modInstance">目标 Mod 实例。</param>
    /// <param name="module">要发送的模块实例。</param>
    /// <param name="toClient">目标客户端 ID，默认为 -1 表示广播。</param>
    /// <param name="ignoreClient">要忽略的客户端 ID，默认为 -1 表示不忽略。</param>
    /// <param name="runLocally">是否在本地也执行该模块的发送逻辑。</param>
    /// <returns>如果发送成功则为 true，否则为 false。</returns>
    public static bool TrySendExternalModule(Mod modInstance, object module, int toClient = -1, int ignoreClient = -1, bool runLocally = false) {
        object[] args = ["NetSimplified_SendModule", module, toClient, ignoreClient, runLocally];
        var reply = modInstance.Call(args);
        if (reply is not bool success) return false;
        return success;
    }

    /// <summary>
    ///     尝试将给定模块实例通过目标模组名称进行发送（用于跨模组的模块传递）。
    /// </summary>
    /// <param name="mod">目标模组的名称。</param>
    /// <param name="module">要发送的模块实例。</param>
    /// <param name="toClient">目标客户端 ID，默认为 -1 表示广播。</param>
    /// <param name="ignoreClient">要忽略的客户端 ID，默认为 -1 表示不忽略。</param>
    /// <param name="runLocally">是否在本地也执行该模块的发送逻辑。</param>
    /// <returns>如果发送成功则为 true，否则为 false。</returns>
    public static bool TrySendExternalModule(string mod, object module, int toClient = -1, int ignoreClient = -1, bool runLocally = false) {
        if (!ModLoader.TryGetMod(mod, out var modInstance)) return false;
        return TrySendExternalModule(modInstance, module, toClient, ignoreClient, runLocally);
    }

    /// <summary>
    ///     尝试从指定模组中按名称获取一个外部 <see cref="FlexibleModule" /> 实例。
    /// </summary>
    /// <param name="mod">目标模组名称。</param>
    /// <param name="name"><see cref="FlexibleModule" /> 的名称（不含 "FlexibleModule." 前缀）。</param>
    /// <param name="module">若成功则输出 <see cref="FlexibleModule" /> 实例，否则为 null。</param>
    /// <returns>如果成功获取则为 true，否则为 false。</returns>
    public static bool TryGetExternalFlexibleModule(string mod, string name, out FlexibleModule module) {
        module = null;
        if (!TryGetExternalModule(mod, $"FlexibleModule.{name}", out var obj)) return false;
        if (obj is not FlexibleModule flexModule) return false;
        module = flexModule;
        return true;
    }

    /// <summary>
    ///     尝试从指定的 Mod 实例中按名称获取一个外部 <see cref="FlexibleModule" /> 实例。
    /// </summary>
    /// <param name="modInstance">目标模组的 Mod 实例。</param>
    /// <param name="name"><see cref="FlexibleModule" /> 的名称（不含 "FlexibleModule." 前缀）。</param>
    /// <param name="module">若成功则输出 <see cref="FlexibleModule" /> 实例，否则为 null。</param>
    /// <returns>如果成功获取则为 true，否则为 false。</returns>
    public static bool TryGetExternalFlexibleModule(Mod modInstance, string name, out FlexibleModule module) {
        module = null;
        if (!TryGetExternalModule(modInstance, $"FlexibleModule.{name}", out var obj)) return false;
        if (obj is not FlexibleModule flexModule) return false;
        module = flexModule;
        return true;
    }

    /// <summary>
    ///     从指定模组中按名称获取一个外部 <see cref="FlexibleModule" /> 实例（若失败返回 null）。
    /// </summary>
    /// <param name="mod">目标模组名称。</param>
    /// <param name="name"><see cref="FlexibleModule" /> 的名称（不含 "FlexibleModule." 前缀）。</param>
    /// <returns>找到的 <see cref="FlexibleModule" /> 实例，或 null。</returns>
    public static FlexibleModule GetExternalFlexibleModule(string mod, string name) {
        TryGetExternalFlexibleModule(mod, name, out var module);
        return module;
    }

    /// <summary>
    ///     从指定 Mod 实例中按名称获取一个外部 <see cref="FlexibleModule" /> 实例（若失败返回 null）。
    /// </summary>
    /// <param name="modInstance">目标模组的 Mod 实例。</param>
    /// <param name="name"><see cref="FlexibleModule" /> 的名称（不含 "FlexibleModule." 前缀）。</param>
    /// <returns>找到的 <see cref="FlexibleModule" /> 实例，或 null。</returns>
    public static FlexibleModule GetExternalFlexibleModule(Mod modInstance, string name) {
        TryGetExternalFlexibleModule(modInstance, name, out var module);
        return module;
    }

    /// <summary>
    ///     将当前模块作为外部模块发送到其它模组/客户端的快捷扩展方法。
    /// </summary>
    /// <param name="module">要发送的模块实例（作为扩展方法的 this 参数）。</param>
    /// <param name="toClient">目标客户端 ID，默认为 -1 表示广播。</param>
    /// <param name="ignoreClient">要忽略的客户端 ID，默认为 -1 表示不忽略。</param>
    /// <param name="runLocally">是否在本地也执行该模块的发送逻辑。</param>
    public static void SendAsExternalModule(this NetModule module, int toClient = -1, int ignoreClient = -1, bool runLocally = false) {
        TrySendExternalModule(module.Mod, module, toClient, ignoreClient, runLocally);
    }
}