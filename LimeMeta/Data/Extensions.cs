using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeSql;
using System.Linq.Expressions;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using LimeMeta.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace LimeMeta.Data;

/// <summary>
/// 扩展方法
/// </summary>
public static class Extensions
{
    private static readonly GeoJsonReader GeoJsonReader = new();

    /// <summary>
    /// FirstOrDefault
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static T? FirstOrDefault<T>(this ISelect<T> source, Expression<Func<T, bool>> exp) => source.Where(exp).First();

    /// <summary>
    /// Merge
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="json"></param>
    public static IEnumerable<string> Merge(this object obj, JObject json)
    {
        var names = new List<string>();

        var type = obj.GetType();
        var pis = type.GetProperties();

        foreach (var jp in json)
        {
            var pi = pis.SingleOrDefault(r => string.Compare(r.Name, jp.Key, true) == 0);
            if (pi == null) continue;

            object? val;
            if (jp.Value == null || jp.Value.Type == JTokenType.Null)
            {
                val = null;
            }
            else if (pi.PropertyType == typeof(string))
            {
                if (jp.Value.Type == JTokenType.String)
                {
                    val = (string)jp.Value!;
                }
                else
                {
                    val = jp.Value!.ToString(Newtonsoft.Json.Formatting.None);
                }
            }
            else if (pi.PropertyType == typeof(JsonElement) || pi.PropertyType == typeof(JsonElement?))
            {
                val = JsonDocument.Parse(jp.Value.ToString(Newtonsoft.Json.Formatting.None)).RootElement;
            }
            else if (typeof(Geometry).IsAssignableFrom(pi.PropertyType))
            {
                val = GeoJsonReader.Read<Geometry>(jp.Value.ToString(Newtonsoft.Json.Formatting.None));
            }
            else
            {
                val = jp.Value.ToObject(pi.PropertyType);
            }

            pi.SetValue(obj, val);
            names.Add(pi.Name);
        }

        return names;
    }

    /// <summary>
    /// Merge
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    public static IEnumerable<string> Merge(this BaseObject obj, BaseDto dto)
    {
        var names = new List<string>();

        var type = obj.GetType();
        var dtoType = dto.GetType();

        foreach (var piDto in dtoType.GetProperties())
        {
            var pi = type.GetProperty(piDto.Name);
            if (pi == null) continue;

            var val = piDto.GetValue(dto);
            if (piDto.Name == nameof(BaseObject.Id) && val == null) continue;

            if (val == null || pi.PropertyType == piDto.PropertyType || pi.PropertyType == Nullable.GetUnderlyingType(piDto.PropertyType))
            {
                pi.SetValue(obj, val);
            }
            else if (pi.PropertyType == typeof(string))
            {
                pi.SetValue(obj, JsonSerializer.Serialize(val, BaseLimeMeta.JsonSerializerOptions));
            }
            else
            {
                throw new NotSupportedException($"name={piDto.Name}, type={piDto.PropertyType.Name}");
            }

            names.Add(piDto.Name);
        }

        return names;
    }
}
