﻿using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GrayMint.Common.Utils;

public static class GmUtil
{
    public static async Task ForEachAsync<T>(T[] source, Func<T, Task> body, int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        foreach (var t in source)
        {
            cancellationToken.ThrowIfCancellationRequested();

            tasks.Add(body(t));
            if (tasks.Count == maxDegreeOfParallelism)
            {
                await Task.WhenAny(tasks);
                foreach (var completedTask in tasks.Where(x => x.IsCompleted).ToArray())
                    tasks.Remove(completedTask);
            }
        }
        await Task.WhenAll(tasks);
    }

    public static T JsonDeserialize<T>(string json, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(json, options) ??
               throw new InvalidDataException($"{typeof(T)} could not be deserialized!");
    }

    public static T JsonClone<T>(object obj, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonDeserialize<T>(json, options);
    }

    public static byte[] GenerateKey()
    {
        return GenerateKey(128);
    }

    public static byte[] GenerateKey(int keySizeInBit)
    {
        using var aes = Aes.Create();
        aes.KeySize = keySizeInBit;
        aes.GenerateKey();
        return aes.Key;
    }

    public static string RedactJsonValue(string json, string[] keys)
    {
        foreach (var key in keys)
        {
            var pattern = "(?<=\"key\":)[^,|}|\r]+(?=,|}|\r)".Replace("key", key);
            json = Regex.Replace(json, pattern, " \"***\"");
        }

        return json;
    }

    public static bool SequenceEqualsOrNull<T>(IEnumerable<T>? left, IEnumerable<T>? right)
    {
        return
            (left == null && right == null) ||
            (left != null && right != null && left.SequenceEqual(right));
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] IEnumerable<T>? array)
    {
        return array == null || !array.Any();
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] T[]? array)
    {
        return array == null || array.Length == 0;
    }
}