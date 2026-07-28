using System.Reflection;
using LimeMeta.Attributes;
using LimeMeta.Models;

namespace LimeMeta.Authorization;

internal enum ModelAuthorizationRequirementKind
{
    Authenticated,
    Permission,
    Denied
}

internal readonly record struct ModelAuthorizationRequirement(
    ModelAuthorizationRequirementKind Kind,
    string? Permission = null);

internal static class ModelAuthorizationPolicy
{
    public static void Validate(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        if (IsSystemModel(modelType))
        {
            return;
        }

        var authorize = modelType.GetCustomAttribute<LimeMetaAuthorizeAttribute>(inherit: true);
        var allowAuthenticated = modelType.GetCustomAttribute<LimeMetaAllowAuthenticatedAttribute>(inherit: true);
        var disableGraphQL = modelType.GetCustomAttribute<DisableGraphQLAttribute>(inherit: true);
        var declarationCount =
            (authorize is null ? 0 : 1) +
            (allowAuthenticated is null ? 0 : 1) +
            (disableGraphQL is null ? 0 : 1);

        if (declarationCount == 0)
        {
            throw new InvalidOperationException(
                $"模型 {modelType.FullName} 会自动生成 GraphQL API，但没有声明权限。请添加 " +
                $"[{nameof(LimeMetaAuthorizeAttribute).Replace("Attribute", string.Empty)}]、" +
                $"[{nameof(LimeMetaAllowAuthenticatedAttribute).Replace("Attribute", string.Empty)}] 或 " +
                $"[{nameof(DisableGraphQLAttribute).Replace("Attribute", string.Empty)}]。");
        }

        if (declarationCount > 1)
        {
            throw new InvalidOperationException(
                $"模型 {modelType.FullName} 只能声明一种自动 GraphQL 访问策略。");
        }

        if (authorize is not null)
        {
            ValidatePermission(modelType, nameof(authorize.Read), authorize.Read);
            ValidatePermission(modelType, nameof(authorize.Create), authorize.Create);
            ValidatePermission(modelType, nameof(authorize.Update), authorize.Update);
            ValidatePermission(modelType, nameof(authorize.Delete), authorize.Delete);
        }

        if (allowAuthenticated is not null &&
            !allowAuthenticated.Read &&
            !allowAuthenticated.Create &&
            !allowAuthenticated.Update &&
            !allowAuthenticated.Delete)
        {
            throw new InvalidOperationException(
                $"模型 {modelType.FullName} 的 [{nameof(LimeMetaAllowAuthenticatedAttribute).Replace("Attribute", string.Empty)}] " +
                "至少需要允许一种操作。");
        }
    }

    public static ModelAuthorizationRequirement Resolve(
        Type modelType,
        LimeMetaOperation operation)
    {
        Validate(modelType);

        if (IsSystemModel(modelType))
        {
            return operation is LimeMetaOperation.Query or LimeMetaOperation.Aggregate
                ? new(ModelAuthorizationRequirementKind.Authenticated)
                : new(ModelAuthorizationRequirementKind.Denied);
        }

        var authorize = modelType.GetCustomAttribute<LimeMetaAuthorizeAttribute>(inherit: true);
        if (authorize is not null)
        {
            var permission = operation switch
            {
                LimeMetaOperation.Query or LimeMetaOperation.Aggregate => authorize.Read,
                LimeMetaOperation.Insert => authorize.Create,
                LimeMetaOperation.Update => authorize.Update,
                LimeMetaOperation.Delete => authorize.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            return new(ModelAuthorizationRequirementKind.Permission, permission);
        }

        var allowAuthenticated =
            modelType.GetCustomAttribute<LimeMetaAllowAuthenticatedAttribute>(inherit: true);
        if (allowAuthenticated is not null)
        {
            var allowed = operation switch
            {
                LimeMetaOperation.Query or LimeMetaOperation.Aggregate => allowAuthenticated.Read,
                LimeMetaOperation.Insert => allowAuthenticated.Create,
                LimeMetaOperation.Update => allowAuthenticated.Update,
                LimeMetaOperation.Delete => allowAuthenticated.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            return allowed
                ? new(ModelAuthorizationRequirementKind.Authenticated)
                : new(ModelAuthorizationRequirementKind.Denied);
        }

        return new(ModelAuthorizationRequirementKind.Denied);
    }

    private static bool IsSystemModel(Type modelType) =>
        modelType.Assembly == typeof(User).Assembly;

    private static void ValidatePermission(Type modelType, string operation, string permission)
    {
        if (!string.IsNullOrWhiteSpace(permission))
        {
            return;
        }

        throw new InvalidOperationException(
            $"模型 {modelType.FullName} 的 {operation} 权限名称不能为空。");
    }
}
