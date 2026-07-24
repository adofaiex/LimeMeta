using LimeMeta.Models;

namespace LimeMeta.GraphQL;

internal sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(user => user.PasswordHash).Ignore();
    }
}
