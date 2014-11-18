// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides a default implementation of <see cref="IControllerActionArgumentBinder"/>.
    /// Uses ModelBinding to populate action parameters.
    /// </summary>
    public class DefaultControllerActionArgumentBinder : IControllerActionArgumentBinder
    {
        private readonly IActionBindingContextProvider _bindingContextProvider;
        private readonly ITypeActivator _typeActivator;

        public DefaultControllerActionArgumentBinder(IActionBindingContextProvider bindingContextProvider, 
                                                     ITypeActivator typeActivator)
        {
            _bindingContextProvider = bindingContextProvider;
            _typeActivator = typeActivator;
        }

        public async Task<IDictionary<string, object>> GetActionArgumentsAsync(ActionContext actionContext)
        {
            var actionBindingContext = await _bindingContextProvider.GetActionBindingContextAsync(actionContext);
            var metadataProvider = actionBindingContext.MetadataProvider;

            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                        nameof(actionContext));
            }

            var parameterMetadata = new List<ModelMetadata>();
            foreach (var parameter in actionDescriptor.Parameters)
            {
                var metadata = metadataProvider.GetMetadataForParameter(
                    modelAccessor: null,
                    methodInfo: actionDescriptor.MethodInfo,
                    parameterName: parameter.Name,
                    binderMetadata: parameter.BinderMetadata);

                if (metadata != null)
                {
                    parameterMetadata.Add(metadata);
                }
            }

            var bodyBoundParameterCount = parameterMetadata.Count(
                                modelMetadata => modelMetadata.BinderMetadata is IFormatterBinderMetadata);
            if (bodyBoundParameterCount > 1)
            {
                throw new InvalidOperationException(Resources.MultipleBodyParametersAreNotAllowed);
            }

            var actionArguments = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var parameter in parameterMetadata)
            {
                await PopulateArgumentAsync(actionBindingContext, actionArguments, parameter);
            }

            return actionArguments;
        }

        private async Task PopulateArgumentAsync(
            ActionBindingContext actionBindingContext,
            IDictionary<string, object> arguments,
            ModelMetadata modelMetadata)
        {

            var parameterType = modelMetadata.ModelType;
            var modelBindingContext = GetModelBindingContext(modelMetadata, actionBindingContext);

            if (await actionBindingContext.ModelBinder.BindModelAsync(modelBindingContext))
            {
                arguments[modelMetadata.PropertyName] = modelBindingContext.Model;
            }
        }

        internal static ModelBindingContext GetModelBindingContext(ModelMetadata modelMetadata,
                                                                   ActionBindingContext actionBindingContext)
        {
            Func<ModelBindingContext, string, bool> propertyFilter =
                (context, propertyName) =>
                {
                    if (BindAttribute.IsPropertyAllowed(propertyName, context.ModelMetadata.IncludedProperties))
                    {
                        var propertyFilterType = context.ModelMetadata.PropertyFilterProviderType;
                        if (propertyFilterType != null)
                        {
                            var requestServices = context.HttpContext.RequestServices;
                            var typeActivator = requestServices.GetService<ITypeActivator>();
                            var propertyFilterProvider = 
                                 (IModelPropertyFilterProvider)typeActivator.CreateInstance(requestServices,
                                                                                            propertyFilterType);
                            return propertyFilterProvider.PropertyFilter(context, propertyName);
                        }

                        return true;
                    }

                    return false;
                };

            var modelBindingContext = new ModelBindingContext
            {
                ModelName = modelMetadata.ModelName ?? modelMetadata.PropertyName,
                ModelMetadata = modelMetadata,
                ModelState = actionBindingContext.ActionContext.ModelState,
                ModelBinder = actionBindingContext.ModelBinder,
                ValidatorProvider = actionBindingContext.ValidatorProvider,
                MetadataProvider = actionBindingContext.MetadataProvider,
                HttpContext = actionBindingContext.ActionContext.HttpContext,
                PropertyFilter = propertyFilter,
                // Fallback only if there is no explicit model name set.
                FallbackToEmptyPrefix = modelMetadata.ModelName == null,
                ValueProvider = actionBindingContext.ValueProvider,
            };

            return modelBindingContext;
        }
    }
}
