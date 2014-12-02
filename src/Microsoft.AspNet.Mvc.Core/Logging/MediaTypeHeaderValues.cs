// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public class MediaTypeHeaderValues : LoggerStructureBase
    {
        public MediaTypeHeaderValues(MediaTypeHeaderValue inner)
        {
            Charset = inner.Charset;
            MediaType = inner.MediaType;
            MediaSubType = inner.MediaSubType;
            MediaTypeRange = inner.MediaTypeRange;
            Parameters = new Dictionary<string, string>(inner.Parameters);
        }

        public string Charset { get; }

        public string MediaType { get; }

        public string MediaSubType { get; }

        public MediaTypeHeaderValueRange MediaTypeRange { get; }

        public IDictionary<string, string> Parameters { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}