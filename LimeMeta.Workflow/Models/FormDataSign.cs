using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LimeMeta.Models;
using FreeSql.DataAnnotations;
using LimeMeta.Attributes;

namespace LimeMeta.Workflow.Models;

/// <summary>
/// FormDataSign
/// </summary>
[Table(Name = "form_data_sign")]
public class FormDataSign : BaseAudit
{
    /// <summary>
    /// 工作流序号
    /// </summary>
    [Column(Name = "workflow_sn"), Indexed]
    public int WorkflowSn { get; set; }

    /// <summary>
    /// 数据状态
    /// </summary>
    [Column(Name = "data_state"), Indexed]
    public required string DataState { get; set; } = FormDataState.Submitted;

    /// <summary>
    /// FormDataId
    /// </summary>
    [Column(Name = "form_data_id"), Indexed]
    public Guid FormDataId { get; set; }

    /// <summary>
    /// FormData
    /// </summary>
    [Navigate(nameof(FormDataId))]
    public FormData? FormData { get; set; }

    /// <summary>
    /// 签核类型
    /// </summary>
    [Column(Name = "sign_type"), Indexed]
    public string SignType { get; set; } = FormDataSignType.FixedSign;

    /// <summary>
    /// 签核序号
    /// </summary>
    [Column(Name = "sign_sn"), Indexed]
    public int SignSn { get; set; }

    /// <summary>
    /// 签核者ID
    /// </summary>
    [Column(Name = "signer_id"), Indexed]
    public Guid SignerId { get; set; }

    /// <summary>
    /// 签核者
    /// </summary>
    [Navigate(nameof(SignerId))]
    public User? Signer { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [Column(Name = "note", StringLength = -1)]
    public string? Note { get; set; }

    /// <summary>
    /// 签核状态
    /// </summary>
    [Column(Name = "sign_state"), Indexed]
    public string SignState { get; set; } = FormDataSignState.Pending;
}

/// <summary>
/// FormDataSignDto
/// </summary>
public class FormDataSignDto : BaseDto
{
    /// <summary>
    /// 工作流序号
    /// </summary>
    public int WorkflowSn { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public required string State { get; set; } = FormDataState.Submitted;

    /// <summary>
    /// FormDataId
    /// </summary>
    public Guid FormDataId { get; set; }

    /// <summary>
    /// 签核类型
    /// </summary>
    public string SignType { get; set; } = FormDataSignType.FixedSign;

    /// <summary>
    /// 签核序号
    /// </summary>
    public int SignSn { get; set; }

    /// <summary>
    /// 签核者ID
    /// </summary>
    public Guid SignerId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// 签核状态
    /// </summary>
    public string SignState { get; set; } = FormDataSignState.Pending;
}
