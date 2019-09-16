#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2017 Google Inc.  All rights reserved.
// https://developers.google.com/protocol-buffers/
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above
// copyright notice, this list of conditions and the following disclaimer
// in the documentation and/or other materials provided with the
// distribution.
//     * Neither the name of Google Inc. nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf.Reflection;
using static Google.Protobuf.Reflection.MessageDescriptor;
using System.Collections.ObjectModel;

namespace Protobuf.Text
{
    internal static partial class WrappersReflection
    {
        /// <summary>
        /// Field number for the single "value" field in all wrapper types.
        /// </summary>
        internal const int WrapperValueFieldNumber = Int32Value.ValueFieldNumber;
    }

    internal static class ProtobufAdapter
    {
        internal const string AnyTypeUrlField = "@type";
        internal const string AnyDiagnosticValueField = "@value";
        internal const string AnyWellKnownTypeValueField = "value";
        internal const string TypeUrlPrefix = "type.googleapis.com";
        private const long BclSecondsAtUnixEpoch = 62135596800;
        internal const long UnixSecondsAtBclMaxValue = 253402300799;
        internal const long UnixSecondsAtBclMinValue = -BclSecondsAtUnixEpoch;

        public const int NanosecondsPerSecond = 1000000000;
        internal const int MaxNanoseconds = NanosecondsPerSecond - 1;
        internal const int MinNanoseconds = -NanosecondsPerSecond + 1;

        internal const int DefaultRecursionLimit = 100;

        private static readonly HashSet<string> WellKnownTypeNames = new HashSet<string>
        {
            "google/protobuf/any.proto",
            "google/protobuf/api.proto",
            "google/protobuf/duration.proto",
            "google/protobuf/empty.proto",
            "google/protobuf/wrappers.proto",
            "google/protobuf/timestamp.proto",
            "google/protobuf/field_mask.proto",
            "google/protobuf/source_context.proto",
            "google/protobuf/struct.proto",
            "google/protobuf/type.proto",
        };

        internal static bool IsWellKnownType(this IDescriptor descriptor)
        {
            var file = descriptor.File;
            return file.Package == "google.protobuf" && WellKnownTypeNames.Contains(file.Name);
        }

        internal static bool IsWrapperType(this IDescriptor descriptor)
        {
            var file = descriptor.File;
            return file.Package == "google.protobuf" && file.Name == "google/protobuf/wrappers.proto";
        }

        internal static IDictionary<string, FieldDescriptor> ByJsonName(this FieldCollection fields)
        {
            var map = new Dictionary<string, FieldDescriptor>();

            foreach (var field in fields.InFieldNumberOrder())
            {
                map[field.Name] = field;
                map[field.JsonName] = field;
            }

            return new ReadOnlyDictionary<string, FieldDescriptor>(map);
        }

        internal static IMessage CreateTemplate(this MessageParser parser)
        {
            var method = parser.GetType().GetMethod("CreateTemplate");
            return (IMessage)method.Invoke(parser, new object[0]);
        }

        internal static bool IsNormalized(long seconds, int nanoseconds)
        {
            // Simple boundaries
            if (seconds < Duration.MinSeconds || seconds > Duration.MaxSeconds ||
                nanoseconds < MinNanoseconds || nanoseconds > MaxNanoseconds)
            {
                return false;
            }
            // We only have a problem is one is strictly negative and the other is
            // strictly positive.
            return Math.Sign(seconds) * Math.Sign(nanoseconds) != -1;
        }
    }
}