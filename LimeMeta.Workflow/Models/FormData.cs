using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Models;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using LimeMeta.Attributes;

namespace LimeMeta.Workflow.Models;

/// <summary>
/// FormData
/// </summary>
[Table(Name = "form_data")]
public class FormData : BaseAudit
{
    /// <summary>
    /// FormId
    /// </summary>
    [Column(Name = "form_id"), Indexed]
    public Guid FormId { get; set; }

    /// <summary>
    /// Form
    /// </summary>
    [Navigate(nameof(FormId))]
    public Form? Form { get; set; }

    /// <summary>
    /// Data
    /// </summary>
    [Column(Name = "data", MapType = typeof(JObject)), Indexed]
    public JsonElement? Data { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [Column(Name = "state"), Indexed]
    public required string State { get; set; } = FormDataState.Submitted;
}

/// <summary>
/// FormDataDto
/// </summary>
public class FormDataDto : BaseDto
{
    /// <summary>
    /// FormId
    /// </summary>
    public Guid FormId { get; set; }

    /// <summary>
    /// Data
    /// </summary>
    public JsonElement? Data { get; set; }
}
