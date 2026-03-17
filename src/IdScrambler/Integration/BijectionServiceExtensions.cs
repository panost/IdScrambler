using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IdScrambler.Serialization;
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
        var registry = new BijectionRegistry();
        configure(registry);
        return RegisterServices(services, registry);
    }

    /// <summary>Load bijection chains from a configuration section.</summary>
    public static IServiceCollection AddBijection(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        var registry = new BijectionRegistry();

        foreach (var child in section.GetChildren())
        {
            var name = child.Key;
            var widthStr = child["width"];
            if (!int.TryParse(widthStr, out int width))
                throw new BijectionConfigException($"Missing or invalid 'width' for chain '{name}'.");

            // Build JSON from the configuration section for the steps
            var stepsSection = child.GetSection("steps");
            var stepsJson = BuildStepsJson(stepsSection);
            var json = $"{{\"width\":{width},\"steps\":{stepsJson}}}";

            if (width == 32)
            {
                var chain = JsonChainReader.Read<uint>(json);
                registry.Register(name, chain);
            }
            else if (width == 64)
            {
                var chain = JsonChainReader.Read<ulong>(json);
                registry.Register(name, chain);
            }
            else
            {
                throw new BijectionConfigException($"Unsupported width {width} for chain '{name}'. Must be 32 or 64.");
            }
        }

        return RegisterServices(services, registry);
    }

    private static IServiceCollection RegisterServices(IServiceCollection services, BijectionRegistry registry)
    {
        services.AddSingleton(registry);

        // Register the JSON type info modifier
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ObfuscatedIdModifier.Apply(registry) }
            };
        });

        // Register the model binder provider
        services.Configure<MvcOptions>(options =>
        {
            options.ModelBinderProviders.Insert(0, new ObfuscatedIdModelBinderProvider());
        });

        return services;
    }

    private static string BuildStepsJson(IConfigurationSection stepsSection)
    {
        var steps = new List<string>();
        foreach (var step in stepsSection.GetChildren())
        {
            var parts = new List<string>();
            foreach (var prop in step.GetChildren())
            {
                if (prop.GetChildren().Any())
                {
                    // Array property
                    var items = prop.GetChildren().Select(c => c.Value!);
                    parts.Add($"\"{prop.Key}\":[{string.Join(",", items)}]");
                }
                else
                {
                    var val = prop.Value!;
                    // Try to determine if the value is numeric
                    if (int.TryParse(val, out _) || val.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        if (val.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            parts.Add($"\"{prop.Key}\":\"{val}\"");
                        else
                            parts.Add($"\"{prop.Key}\":{val}");
                    }
                    else
                    {
                        parts.Add($"\"{prop.Key}\":\"{val}\"");
                    }
                }
            }
            steps.Add($"{{{string.Join(",", parts)}}}");
        }
        return $"[{string.Join(",", steps)}]";
    }
}
