using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NetSimplified.Syncing;

internal static class AutoSyncHandler
{
    public static Dictionary<Type, AutoSyncType> RegisteredAutoSyncTypes = new();

    private static bool IsNonNullableValueType(Type t) {
        return t.IsValueType && Nullable.GetUnderlyingType(t) == null;
    }

    public static void RegisterType(AutoSyncType type) {
        if (type == null) return;
        RegisteredAutoSyncTypes[type.Type] = type;
    }

    // Helper: 发送单个值（先尝试已注册类型，再处理集合/数组）
    public static void SendValue(BinaryWriter bw, object value, Type declaredType, MemberInfo fieldInfo = null) {
        // 统一 null 支持
        var needsNullMarker = !IsNonNullableValueType(declaredType);
        if (needsNullMarker) {
            bw.Write(value != null);
            if (value == null) return;
        }

        // 声明类型的 handler 优先
        if (RegisteredAutoSyncTypes.TryGetValue(declaredType, out var handler)) {
            handler.Send(bw, value, fieldInfo);
            return;
        }

        // 数组（支持多维数组 n-dim）
        if (value is Array arr) {
            var elemType = arr.GetType().GetElementType() ?? declaredType;
            // 写入维度数和每个维度的长度（便于支持多维数组）
            var rank = (byte) arr.Rank;
            bw.Write(rank);
            var lengths = new int[rank];
            for (var i = 0; i < rank; i++) {
                lengths[i] = arr.GetLength(i);
                bw.Write(lengths[i]);
            }

            // 按行优先顺序扁平化写入所有元素
            foreach (var it in arr) SendValue(bw, it!, elemType, fieldInfo);
            return;
        }

        // KeyValuePair<TKey, TValue>
        if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
            var args = declaredType.GetGenericArguments();
            var keyType = args[0];
            var valueType = args[1];

            if (value == null) {
                // KeyValuePair is a value type; null only possible if boxed null, handle gracefully
                SendValue(bw, null, keyType, fieldInfo);
                SendValue(bw, null, valueType, fieldInfo);
                return;
            }

            var ptype = value.GetType();
            var key = ptype.GetProperty("Key")!.GetValue(value);
            var val = ptype.GetProperty("Value")!.GetValue(value);

            SendValue(bw, key!, keyType, fieldInfo);
            SendValue(bw, val!, valueType, fieldInfo);
            return;
        }

        // IEnumerable<T>
        var enumInterfaceDeclared = declaredType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumInterfaceDeclared != null) {
            var elemType = enumInterfaceDeclared.GetGenericArguments()[0];
            var enumerable = (IEnumerable) value!;
            var temp = enumerable.Cast<object?>().ToList();
            bw.Write(temp.Count);
            foreach (var it in temp) SendValue(bw, it!, elemType, fieldInfo);
        }

        // 未注册 handler 且非集合/数组时不写入任何数据（由用户自定义处理）
    }

    // 读取单个值（声明类型优先，期望存在 null 标记）
    internal static object ReadValue(BinaryReader r, Type declaredType, MemberInfo fieldInfo = null) {
        // 统一 null 支持
        var needsNullMarker = !IsNonNullableValueType(declaredType);
        if (needsNullMarker) {
            var has = r.ReadBoolean();
            if (!has) return null;
        }

        // 已注册 handler（声明类型优先）
        if (RegisteredAutoSyncTypes.TryGetValue(declaredType, out var handler)) return handler.Read(r, fieldInfo);

        // 数组（支持多维数组 n-dim）
        if (declaredType.IsArray) {
            var elemType = declaredType.GetElementType();
            // 读取维度和每个维度长度
            var rank = r.ReadByte();
            var lengths = new int[rank];
            var total = 1;
            for (var i = 0; i < rank; i++) {
                lengths[i] = r.ReadInt32();
                total *= lengths[i];
            }

            var arr = Array.CreateInstance(elemType!, lengths);

            // 扁平读取并按行优先顺序写入到多维数组中
            for (var flat = 0; flat < total; flat++) {
                var value = ReadValue(r, elemType!, fieldInfo);
                var indices = new int[rank];
                var rem = flat;
                for (var d = rank - 1; d >= 0; d--) {
                    var len = lengths[d];
                    indices[d] = rem % len;
                    rem /= len;
                }

                arr.SetValue(value, indices);
            }

            return arr;
        }

        // KeyValuePair<TKey,TValue>
        if (declaredType.IsGenericType && declaredType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
            var args = declaredType.GetGenericArguments();
            var keyType = args[0];
            var valueType = args[1];
            var key = ReadValue(r, keyType, fieldInfo);
            var val = ReadValue(r, valueType, fieldInfo);
            var kvType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            return Activator.CreateInstance(kvType, key, val)!;
        }

        // IEnumerable<T>（支持嵌套）
        var enumInterface = declaredType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumInterface != null) {
            var elemType = enumInterface.GetGenericArguments()[0];

            // 读取元素数量
            var len = r.ReadInt32();

            // 尝试创建目标集合类型
            object collection;
            try {
                collection = Activator.CreateInstance(declaredType) ?? Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));
            }
            catch {
                collection = Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));
            }

            var add = collection.GetType().GetMethod("Add");
            if (add != null) {
                for (var i = 0; i < len; i++) add.Invoke(collection, new[] { ReadValue(r, elemType, fieldInfo) });
                return collection;
            }

            // 无 Add 时回退为 List<T>
            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (IList) Activator.CreateInstance(listType)!;
            for (var i = 0; i < len; i++) list.Add(ReadValue(r, elemType, fieldInfo));
            return list;
        }

        // 不支持，返回 null（自动忽视）
        return null;
    }

    internal static void HandleAutoSend(NetModule netModule, BinaryWriter bw) {
        if (!NetModuleLoader.FieldInfos.TryGetValue(netModule.Name, out var fields)) return;

        foreach (var fieldInfo in fields) {
            var value = fieldInfo.GetValue(netModule);
            var declared = fieldInfo.FieldType;

            SendValue(bw, value, declared, fieldInfo);
        }
    }

    internal static void HandleAutoRead(NetModule netModule, BinaryReader r) {
        if (!NetModuleLoader.FieldInfos.TryGetValue(netModule.Name, out var fields)) return;

        foreach (var fieldInfo in fields) {
            var declared = fieldInfo.FieldType;
            var value = ReadValue(r, declared, fieldInfo);
            fieldInfo.SetValue(netModule, value);
        }
    }
}