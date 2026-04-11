using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace NetSimplified.Syncing;

/// <summary>
///     此特性允许变量自动传输 <br/>
///     自动传输的变量必须有对应的注册了的 <see cref="AutoSyncType"/> 类，或者是仅含已注册的变量的 <see cref="IEnumerable"/>, <see cref="KeyValuePair"/> 或 <see cref="Array"/>。<br/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
public class AutoSyncAttribute : Attribute
{
}