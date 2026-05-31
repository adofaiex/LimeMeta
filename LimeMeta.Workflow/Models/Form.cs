using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;
using LimeMeta.Models;

namespace LimeMeta.Workflow.Models;

/// <summary>
/// Form
/// </summary>
[Table(Name = "form")]
public class Form : BaseAudit
{
    /// <summary>
    /// Name
    /// </summary>
    [Column(Name = "name"), Indexed]
    public required string Name { get; set; }

    /// <summary>
    /// markdown 描述的工作流
    /// </summary>
    [Column(Name = "workflow", StringLength = -1)]
    public required string Workflow { get; set; }
}


/// <summary>
/// FormDto
/// </summary>
public class FormDto : BaseDto
{
    /// <summary>
    /// Name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// markdown 描述的工作流
    /// </summary>
    public required string Workflow { get; set; }
}
