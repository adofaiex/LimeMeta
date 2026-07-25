using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using System.Text.Json.Serialization;

namespace LimeMeta.Models;

/// <summary>
/// 用户
/// </summary>
[Table(Name = "user")]
public class User : BaseAudit
{
    /// <summary>
    /// 名称
    /// </summary>
    [Column(Name = "name"), Indexed]
    public required string Name { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [Column(Name = "uname"), Indexed]
    public required string Username { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Column(Name = "pwd"), Indexed]
    [JsonIgnore]
    public required string PasswordHash { get; set; }

    /// <summary>
    /// 手机
    /// </summary>
    [Column(Name = "phone"), Indexed]
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [Column(Name = "email", StringLength = 200), Indexed]
    public string? Email { get; set; }

    /// <summary>
    /// 头像文件 ID
    /// </summary>
    [Column(Name = "avatar_file_id"), Indexed]
    public Guid? AvatarFileId { get; set; }

    /// <summary>
    /// 角色
    /// </summary>
    [Navigate(ManyToMany = typeof(UserRole))]
    public List<Role> Roles { get; set; } = [];

    /// <summary>
    /// 消息
    /// </summary>
    [Navigate(ManyToMany = typeof(MessageUser))]
    public List<Message> Messages { get; set; } = [];
}

/// <summary>
/// UserDto
/// </summary>
public class UserDto : BaseDto
{
    public required string Name { get; set; }

    public required string Username { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public Guid? AvatarFileId { get; set; }
}
