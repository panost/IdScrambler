using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace IdScrambler.Integration;

/// <summary>
/// Model binder that decodes obfuscated IDs from route/query parameters.
/// </summary>
public sealed class ObfuscatedIdModelBinder : IModelBinder
{
    private readonly BijectionRegistry _registry;
    private readonly string _chainName;
    private readonly ObfuscatedIdFormat _format;
    private readonly Type _targetType;

    public ObfuscatedIdModelBinder(BijectionRegistry registry, string chainName,
        ObfuscatedIdFormat format, Type targetType)
    {
        _registry = registry;
        _chainName = chainName;
        _format = format;
        _targetType = targetType;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        var token = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(token))
            return Task.CompletedTask;

        try
        {
            object result;
            if (_targetType == typeof(int))
            {
                result = _registry.DecodeInt32(_chainName, token, _format);
            }
            else if (_targetType == typeof(long))
            {
                result = _registry.DecodeInt64(_chainName, token, _format);
            }
            else
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                    $"Unsupported type for obfuscated ID: {_targetType.Name}");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception ex)
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName,
                $"Invalid obfuscated ID: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Model binder provider for [ObfuscatedId]-annotated parameters.
/// </summary>
public sealed class ObfuscatedIdModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var attr = context.Metadata is Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadata metadata
            ? metadata.Attributes.ParameterAttributes
                ?.OfType<ObfuscatedIdAttribute>()
                .FirstOrDefault()
            : null;

        if (attr == null) return null;

        if (context.Metadata.ModelType != typeof(int) && context.Metadata.ModelType != typeof(long))
            return null;

        // Resolve the registry from DI — we need to defer this until the binder is created
        return new DeferredModelBinder(attr.ChainName, attr.Format, context.Metadata.ModelType);
    }

    private sealed class DeferredModelBinder : IModelBinder
    {
        private readonly string _chainName;
        private readonly ObfuscatedIdFormat _format;
        private readonly Type _targetType;

        public DeferredModelBinder(string chainName, ObfuscatedIdFormat format, Type targetType)
        {
            _chainName = chainName;
            _format = format;
            _targetType = targetType;
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var registry = bindingContext.HttpContext.RequestServices
                .GetRequiredService(typeof(BijectionRegistry)) as BijectionRegistry
                ?? throw new InvalidOperationException("BijectionRegistry not registered in DI.");

            var binder = new ObfuscatedIdModelBinder(registry, _chainName, _format, _targetType);
            return binder.BindModelAsync(bindingContext);
        }
    }
}
