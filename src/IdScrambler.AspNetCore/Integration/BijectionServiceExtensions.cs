using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IdScrambler.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdScrambler.Integration;

/// <summary>
/// Extension methods for registering bijection chains with ASP.NET Core DI.
/// </summary>
public static class BijectionServiceExtensions
{
    /// <summary>Register bijection chains programmatically.</summary>
    public static IServiceCollection AddBijection(
        this IServiceCollection services,
        Action<BijectionRegistry> configure)
    {
        var registry = GetOrCreateRegistry(services);
        configure(registry);
        return RegisterServices(services, registry);
    }

    /// <summary>Load bijection chains from a configuration section.</summary>
    public static IServiceCollection AddBijection(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        var registry = GetOrCreateRegistry(services);

        foreach (var child in section.GetChildren())
        {
            var name = child.Key;
            var widthStr = child["width"];
            if (!int.TryParse(widthStr, out int width))
                throw new BijectionConfigException($"Missing or invalid 'width' for chain '{name}'.");

            // Build JSON from the configuration section for the steps using safe serialization
            var stepsSection = child.GetSection("steps");
            var json = BuildSafeJson(width, stepsSection);

            if (width == 16)
            {
                var chain = BijectionSerializer.FromJson<ushort>(json);
                registry.Register(name, chain);
            }
            else if (width == 32)
            {
                var chain = BijectionSerializer.FromJson<uint>(json);
                registry.Register(name, chain);
            }
            else if (width == 64)
            {
                var chain = BijectionSerializer.FromJson<ulong>(json);
                registry.Register(name, chain);
            }
            else
            {
                throw new BijectionConfigException($"Unsupported width {width} for chain '{name}'. Must be 16, 32, or 64.");
            }
        }

        return RegisterServices(services, registry);
    }

    private static BijectionRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(BijectionRegistry));
        if (descriptor?.ImplementationInstance is BijectionRegistry existing)
            return existing;
        return new BijectionRegistry();
    }

    private static IServiceCollection RegisterServices(IServiceCollection services, BijectionRegistry registry)
    {
        // Remove any existing registrations to avoid duplicates
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(BijectionRegistry));
        if (existing != null)
            services.Remove(existing);

        services.AddSingleton(registry);

        // Only add JSON configuration and model binder once
        if (!services.Any(d => d.ServiceType == typeof(BijectionServicesMarker)))
        {
            services.AddSingleton<BijectionServicesMarker>();

            services.ConfigureHttpJsonOptions(options => ConfigureJsonOptions(options.SerializerOptions, registry));
            services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
                ConfigureJsonOptions(options.JsonSerializerOptions, registry));

            services.Configure<MvcOptions>(options =>
            {
                options.ModelBinderProviders.Insert(0, new ObfuscatedIdModelBinderProvider());
            });
        }

        return services;
    }

    // Marker class to detect duplicate registration
    private sealed class BijectionServicesMarker;

    private static void ConfigureJsonOptions(JsonSerializerOptions options, BijectionRegistry registry)
    {
        var modifier = ObfuscatedIdModifier.Apply(registry);

        if (options.TypeInfoResolver is DefaultJsonTypeInfoResolver defaultResolver)
        {
            defaultResolver.Modifiers.Add(modifier);
            return;
        }

        var modifierResolver = new DefaultJsonTypeInfoResolver();
        modifierResolver.Modifiers.Add(modifier);

        options.TypeInfoResolver = options.TypeInfoResolver is null
            ? modifierResolver
            : JsonTypeInfoResolver.Combine(options.TypeInfoResolver, modifierResolver);
    }

    private static string BuildSafeJson(int width, IConfigurationSection stepsSection)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WriteNumber("width", width);
        writer.WriteStartArray("steps");

        foreach (var step in stepsSection.GetChildren())
        {
            writer.WriteStartObject();
            foreach (var prop in step.GetChildren())
            {
                if (prop.GetChildren().Any())
                {
                    // Array property
                    writer.WriteStartArray(prop.Key);
                    foreach (var item in prop.GetChildren())
                        writer.WriteRawValue(item.Value ?? "0");
                    writer.WriteEndArray();
                }
                else
                {
                    var val = prop.Value ?? "";
                    if (int.TryParse(val, out int intVal))
                    {
                        writer.WriteNumber(prop.Key, intVal);
                    }
                    else
                    {
                        writer.WriteString(prop.Key, val);
                    }
                }
            }
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}
