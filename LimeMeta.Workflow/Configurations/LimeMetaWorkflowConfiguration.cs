using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LimeMeta.Workflow.Configurations;

/// <summary>
/// LimeMetaWorkflowConfiguration
/// </summary>
public class LimeMetaWorkflowConfiguration
{
    /// <summary>
    /// EngineUrl
    /// </summary>
    public required string EngineUrl { get; set; }

    /// <summary>
    /// EngineKey
    /// </summary>
    public required string EngineKey { get; set; }
}
