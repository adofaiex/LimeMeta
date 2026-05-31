using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LimeMeta.Models;

namespace LimeMeta.Logics;

/// <summary>
/// 逻辑管理器接口
/// </summary>
public interface ILogicManager
{
    /// <summary>
    /// 模型类型
    /// </summary>
    IEnumerable<Type> ModelTypes { get; }

    /// <summary>
    /// 逻辑列表
    /// </summary>
    IEnumerable<BaseLogic> Logics { get; }

    /// <summary>
    /// ModelMapper
    /// </summary>
    /// <value></value>
    IMapper ModelMapper { get; }

    /// <summary>
    /// RaiseBeforeSelectEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseBeforeSelectEvent<T>(BeforeSelectEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseAfterSelectEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseAfterSelectEvent<T>(AfterSelectEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseBeforeInsertEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseBeforeInsertEvent<T>(BeforeInsertEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseAfterInsertEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseAfterInsertEvent<T>(AfterInsertEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseBeforeUpdateEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseBeforeUpdateEvent<T>(BeforeUpdateEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseAfterUpdateEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseAfterUpdateEvent<T>(AfterUpdateEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseBeforeDeleteEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseBeforeDeleteEvent<T>(BeforeDeleteEventArgs<T> args) where T : class;

    /// <summary>
    /// RaiseAfterDeleteEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    void RaiseAfterDeleteEvent<T>(AfterDeleteEventArgs<T> args) where T : class;

    /// <summary>
    /// GetModelDtoType
    /// </summary>
    /// <param name="modelType"></param>
    /// <returns></returns>
    Type GetModelDtoType(Type modelType);
}
