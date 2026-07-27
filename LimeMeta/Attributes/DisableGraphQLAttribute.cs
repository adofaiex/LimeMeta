using System;

namespace LimeMeta.Attributes;

/// <summary>
/// 禁止为模型自动生成 GraphQL 查询、聚合和增删改根字段。
/// 模型仍参与 LimeMeta 的表结构同步、Seed、Logic 和数据操作。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class DisableGraphQLAttribute : Attribute
{
}
