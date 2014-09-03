﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies the contracts for caching view locations generated by <see cref="IViewLocationExpander"/>.
    /// </summary>
    public interface IViewLocationCache
    {
        /// <summary>
        /// Gets a cached view location based on the specified <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewLocationExpanderContext"/> for the current view location 
        /// expansion.</param>
        /// <returns>The cached location, if available, <c>null</c> otherwise.</returns>
        string Get(ViewLocationExpanderContext context);

        /// <summary>
        /// Adds a cache entry for values specified by <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="ViewLocationExpanderContext"/> for the current view location 
        /// expansion.</param>
        /// <param name="value">The view location that is to be cached.</param>
        void Set(ViewLocationExpanderContext context, string value);
    }
}