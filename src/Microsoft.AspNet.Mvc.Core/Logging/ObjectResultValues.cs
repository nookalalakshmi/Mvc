// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class ObjectResultValues : LoggerStructureBase
    {
        public ObjectResultValues(ObjectResult inner, HttpContext context, IOutputFormatter selected)
        {
            Value = inner.Value.GetType();
            Formatters = inner.Formatters.Select(f => new OutputFormatterValues(f));
            ContentTypes = inner.ContentTypes.Select(c => new MediaTypeHeaderValues(c));
            DeclaredType = inner.DeclaredType;
            SelectedFormatter = new OutputFormatterValues(selected);
            AcceptHeader = context.Request.Headers["Accept"];
            ContentTypeHeader = context.Request.Headers["Content-Type"];
        }

        public Type Value { get; }

        public IEnumerable<OutputFormatterValues> Formatters { get; }

        public IEnumerable<MediaTypeHeaderValues> ContentTypes { get; }

        public Type DeclaredType { get; }

        public OutputFormatterValues SelectedFormatter { get; }

        public string AcceptHeader { get; }

        public string ContentTypeHeader { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}