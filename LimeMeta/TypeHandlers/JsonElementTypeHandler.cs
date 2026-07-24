using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LimeMeta.TypeHandlers;

/// <summary>
/// JsonElementTypeHandler
/// </summary>
internal sealed class JsonElementTypeHandler : TypeHandler<JsonElement>
{
    public override JsonElement Deserialize(object value)
    {
        if (value is string s)
        {
            return JsonDocument.Parse(s).RootElement;
        }

        return JsonDocument.Parse(value.ToString()!).RootElement;
    }

    public override object Serialize(JsonElement value) => value.ToString();
}

