using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LimeMeta.Workflow.Models;
using LimeMeta.Logics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace LimeMeta.Workflow.Logics;

/// <summary>
/// FormDataLogic
/// </summary>
public sealed class FormDataLogic : BaseLogic<FormData>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FormDataLogic> _logger;

    /// <summary>
    /// FormDataLogic
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="scopeFactory"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="configuration"></param>
    public FormDataLogic(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration) : base(loggerFactory, scopeFactory)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<FormDataLogic>();
        AfterInsert += OnAfterInsertAsync;
        AfterUpdate += OnAfterUpdateAsync;
    }

    /// <summary>
    /// OnAfterInsertAsync - 触发 MAF 工作流（首次启动）
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAfterInsertAsync(object? sender, AfterInsertEventArgs<FormData> e)
    {
        await TriggerWorkflowAsync(e.Objs, "run");
    }

    /// <summary>
    /// OnAfterUpdateAsync - 推进 MAF 工作流
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnAfterUpdateAsync(object? sender, AfterUpdateEventArgs<FormData> e)
    {
        await TriggerWorkflowAsync(e.Objs.Values, "advance");
    }

    /// <summary>
    /// TriggerWorkflowAsync - 触发工作流的通用方法
    /// </summary>
    /// <param name="objs"></param>
    /// <param name="action">动作类型：run（首次启动）或 advance（推进）</param>
    private async Task TriggerWorkflowAsync(IEnumerable<FormData> objs, string action = "run")
    {
        var mafEndpoint = _configuration["LimeMetaWorkflow:EngineUrl"] ?? "http://maf:8000/workflow";
        var client = _httpClientFactory.CreateClient("MAF");

        foreach (var obj in objs)
        {
            try
            {
                var payload = new { form_data_id = obj.Id.ToString() };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var endpoint = action == "advance" ? $"{mafEndpoint}/advance" : $"{mafEndpoint}/run";
                var response = await client.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully {Action} MAF workflow for FormData {Id}", action, obj.Id);
                }
                else
                {
                    _logger.LogWarning("MAF workflow {Action} returned non-success status for FormData {Id}: {StatusCode}",
                        action, obj.Id, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to {Action} MAF workflow for FormData {Id}", action, obj.Id);
                // TODO: Implement retry queue or dead letter queue
            }
        }
    }
}
