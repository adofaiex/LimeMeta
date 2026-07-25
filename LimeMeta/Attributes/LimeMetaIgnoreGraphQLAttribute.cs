namespace LimeMeta.Attributes;

/// <summary>
/// 标记模型参与表结构同步，但不生成自动 GraphQL CRUD。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class LimeMetaIgnoreGraphQLAttribute : Attribute
{
}
