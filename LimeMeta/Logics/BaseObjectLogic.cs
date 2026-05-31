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
/// 基础对象逻辑
/// </summary>
sealed class BaseObjectLogic : BaseLogic<IBaseObject>
{
    public BaseObjectLogic(ILoggerFactory loggerFactory, IServiceScopeFactory scopeFactory) : base(loggerFactory, scopeFactory)
    {
        Order = 0;
    }
}
