using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LimeMeta.Models;
using LimeMeta.Attributes;

namespace LimeMeta.Logics;

/// <summary>
/// 审计对象逻辑
/// </summary>
sealed class AuditObjectLogic : BaseLogic<IAuditObject>
{
    public AuditObjectLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        BeforeInsert += OnBeforeInsert;
        BeforeUpdate += OnBeforeUpdate;
    }

    /// <summary>
    /// OnBeforeInsert
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnBeforeInsert(object? sender, BeforeInsertEventArgs<IAuditObject> e)
    {
        foreach (var obj in e.Objs)
        {
            obj.Created = DateTime.Now.ToReadableLong();
            obj.CreatorId = e.UserId;

            obj.Updated = DateTime.Now.ToReadableLong();
            obj.ModifierId = e.UserId;
        }
    }

    /// <summary>
    /// OnBeforeUpdate
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnBeforeUpdate(object? sender, BeforeUpdateEventArgs<IAuditObject> e)
    {
        foreach (var (_, newObj) in e.Objs)
        {
            newObj.Updated = DateTime.Now.ToReadableLong();
            newObj.ModifierId = e.UserId;
        }
    }
}
