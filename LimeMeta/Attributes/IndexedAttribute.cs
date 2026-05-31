using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimeMeta.Attributes;

/// <summary>
/// 索引
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class IndexedAttribute : Attribute
{
}
