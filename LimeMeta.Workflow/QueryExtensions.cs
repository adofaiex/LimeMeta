using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;
using LimeMeta.GraphQL;

namespace LimeMeta.Workflow;

/// <summary>
/// QueryExtensions
/// </summary>
[ExtendObjectType(typeof(Query))]
public class QueryExtensions
{
    public string Hello() => "Hello, World!";
}
