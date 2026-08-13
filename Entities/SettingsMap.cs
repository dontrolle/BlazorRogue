using System;
using System.Collections.Generic;

namespace BlazorRogue.Entities;

/// <summary>
/// A JSON-like value tree restricted to int, double, string, and nested maps of the same - exactly
/// what map-generator parameters in levels.json are allowed to be.
/// </summary>
/// <remarks>
/// Doesn't reference System.Text.Json - only <see cref="Configuration"/>, which builds these from
/// JsonElement, needs to know JSON exists. Consumers (e.g. map generators) read through the typed
/// getters below instead.
/// </remarks>
class SettingsMap(IReadOnlyDictionary<string, object> values)
{
    public static readonly SettingsMap Empty = new(new Dictionary<string, object>());

    public int GetInt(string key) => GetRequired<int>(key, "int");

    public int GetInt(string key, int defaultValue) => GetOrDefault(key, defaultValue);

    public string GetString(string key) => GetRequired<string>(key, "string");

    public string GetString(string key, string defaultValue) => GetOrDefault(key, defaultValue);

    public SettingsMap GetMap(string key) => GetRequired<SettingsMap>(key, "map");

    public SettingsMap GetMap(string key, SettingsMap defaultValue) =>
        GetOrDefault(key, defaultValue);

    // int is a valid double (1 == 1.0), so this widens; GetInt() intentionally does not accept a
    // double back, since that could silently truncate a fractional value.
    public double GetDouble(string key) =>
        values.TryGetValue(key, out object? value)
            ? value switch
            {
                double d => d,
                int i => i,
                _ => throw TypeError(key, "double", value),
            }
            : throw MissingError(key);

    public double GetDouble(string key, double defaultValue) =>
        values.TryGetValue(key, out object? value)
            ? value switch
            {
                double d => d,
                int i => i,
                _ => defaultValue,
            }
            : defaultValue;

    T GetRequired<T>(string key, string kind) =>
        values.TryGetValue(key, out object? value)
            ? (value is T typed ? typed : throw TypeError(key, kind, value))
            : throw MissingError(key);

    T GetOrDefault<T>(string key, T defaultValue) =>
        values.TryGetValue(key, out object? value) && value is T typed ? typed : defaultValue;

    static InvalidOperationException MissingError(string key) =>
        new($"Missing required setting '{key}'.");

    static InvalidOperationException TypeError(string key, string kind, object actual) =>
        new(
            $"Setting '{key}' was expected to have a {kind} value, but was {actual.GetType().Name}."
        );
}
