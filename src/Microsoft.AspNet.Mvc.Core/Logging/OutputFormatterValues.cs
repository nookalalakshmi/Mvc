// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class OutputFormatterValues : LoggerStructureBase
    {
        public OutputFormatterValues(IOutputFormatter inner)
        {
            var formatter = inner as OutputFormatter;
            if (formatter != null)
            {
                SupportedEncodings = new List<Encoding>(formatter.SupportedEncodings);
                SupportedMediaTypes = formatter.SupportedMediaTypes.Select(s => new MediaTypeHeaderValues(s));
            }

            OutputFormatterType = inner?.GetType();
        }

        public Type OutputFormatterType { get; }

        public IEnumerable<Encoding> SupportedEncodings { get; }

        public IEnumerable<MediaTypeHeaderValues> SupportedMediaTypes { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}