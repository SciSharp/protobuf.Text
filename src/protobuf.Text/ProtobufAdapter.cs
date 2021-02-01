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
using System.Reflection;
using System.Text;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Collections;

namespace Protobuf.Text
{
    internal static partial class WrappersReflection
    {
        /// <summary>
        /// Field number for the single "value" field in all wrapper types.
        /// </summary>
        internal const int WrapperValueFieldNumber = Int32Value.ValueFieldNumber;
    }

    public static class ProtobufAdapter
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

        internal static bool IsValueType(this IMessage value)
        {
            if (value is Google.Protobuf.WellKnownTypes.Timestamp)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.Struct)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.StringValue)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.Int32Value)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.UInt32Value)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.Int64Value)
                return true;

             if (value is Google.Protobuf.WellKnownTypes.UInt64Value)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.BoolValue)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.BytesValue)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.DoubleValue)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.FloatValue)
                return true;

            if (value is Google.Protobuf.WellKnownTypes.EnumValue)
                return true;

            return false;
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
            var method = parser.GetType().GetMethod("CreateTemplate", BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
                throw new Exception("Cannot find method 'CreateTemplate'");

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

        internal static bool IsExtension(this FieldDescriptor descriptor)
        {
            return false;
        }

        public static string ToText<T>(this IMessage<T> msg)
            where T : IMessage<T>
        {
            return TextFormatter.Default.Format(msg);
        }

        public static T ParseText<T>(this MessageParser<T> parser, string text)
            where T : IMessage<T>
        {
            var message = (T)parser.CreateTemplate();
            TextParser.Default.Merge(message, text);
            return message;
        }

        public static IMessage ParseText(this MessageParser parser, string text)
        {
            var message = parser.CreateTemplate();
            TextParser.Default.Merge(message, text);
            return message;
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void AppendNanoseconds(StringBuilder builder, int nanos)
        {
            if (nanos != 0)
            {
                builder.Append('.');
                // Output to 3, 6 or 9 digits.
                if (nanos % 1000000 == 0)
                {
                    builder.Append((nanos / 1000000).ToString("d3", CultureInfo.InvariantCulture));
                }
                else if (nanos % 1000 == 0)
                {
                    builder.Append((nanos / 1000).ToString("d6", CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append(nanos.ToString("d9", CultureInfo.InvariantCulture));
                }
            }
        }
        internal static string TimestampToText(long seconds, int nanoseconds, bool diagnosticOnly)
        {
            if (IsNormalized(seconds, nanoseconds))
            {
                // Use .NET's formatting for the value down to the second, including an opening double quote (as it's a string value)
                DateTime dateTime = UnixEpoch.AddSeconds(seconds);
                var builder = new StringBuilder();
                builder.Append('"');
                builder.Append(dateTime.ToString("yyyy'-'MM'-'dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
                AppendNanoseconds(builder, nanoseconds);
                builder.Append("Z\"");
                return builder.ToString();
            }

            if (diagnosticOnly)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{{ \"@warning\": \"Invalid Timestamp\", \"seconds\": \"{0}\", \"nanos\": {1} }}",
                    seconds,
                    nanoseconds);
            }
            else
            {
                throw new InvalidOperationException("Non-normalized timestamp value");
            }
        }

        internal static string DurationToText(long seconds, int nanoseconds, bool diagnosticOnly)
        {
            if (IsNormalized(seconds, nanoseconds))
            {
                var builder = new StringBuilder();
                builder.Append('"');
                // The seconds part will normally provide the minus sign if we need it, but not if it's 0...
                if (seconds == 0 && nanoseconds < 0)
                {
                    builder.Append('-');
                }

                builder.Append(seconds.ToString("d", CultureInfo.InvariantCulture));
                AppendNanoseconds(builder, Math.Abs(nanoseconds));
                builder.Append("s\"");
                return builder.ToString();
            }
            if (diagnosticOnly)
            {
                // Note: the double braces here are escaping for braces in format strings.
                return string.Format(CultureInfo.InvariantCulture,
                    "{{ \"@warning\": \"Invalid Duration\", \"seconds\": \"{0}\", \"nanos\": {1} }}",
                    seconds,
                    nanoseconds);
            }
            else
            {
                throw new InvalidOperationException("Non-normalized duration value");
            }
        }

        private static bool IsPathValid(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c >= 'A' && c <= 'Z')
                {
                    return false;
                }
                if (c == '_' && i < input.Length - 1)
                {
                    char next = input[i + 1];
                    if (next < 'a' || next > 'z')
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        internal static string FieldMaskToText(IList<string> paths, bool diagnosticOnly)
        {
            var firstInvalid = paths.FirstOrDefault(p => !IsPathValid(p));
            if (firstInvalid == null)
            {
                var writer = new StringWriter();
#if NET35
                var query = paths.Select(TextFormatter.ToTextName);
                TextFormatter.WriteString(writer, string.Join(",", query.ToArray()));
#else
                TextFormatter.WriteString(writer, string.Join(",", paths.Select(TextFormatter.ToTextName)));
#endif
                return writer.ToString();
            }
            else
            {
                if (diagnosticOnly)
                {
                    var writer = new StringWriter();
                    writer.Write("{ \"@warning\": \"Invalid FieldMask\", \"paths\": ");
                    TextFormatter.Default.WriteList(writer, (IList)paths);
                    writer.Write(" }");
                    return writer.ToString();
                }
                else
                {
                    throw new InvalidOperationException($"Invalid field mask to be converted to JSON: {firstInvalid}");
                }
            }
        }
    }
}