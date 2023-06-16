#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2015 Google Inc.  All rights reserved.
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
using System.Collections;
using System.Globalization;
using System.Text;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Google.Protobuf;

namespace Protobuf.Text
{
    /// <summary>
    /// Reflection-based converter from messages to TEXT.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this class are thread-safe, with no mutable state.
    /// </para>
    /// <para>
    /// This is a simple start to get text formatting working. As it's reflection-based,
    /// it's not as quick as baking calls into generated messages - but is a simpler implementation.
    /// (This code is generally not heavily optimized.)
    /// </para>
    /// </remarks>
    public sealed class TextFormatter
    {
        internal const string AnyTypeUrlField = "@type";
        internal const string AnyDiagnosticValueField = "@value";
        internal const string AnyWellKnownTypeValueField = "value";
        private const string TypeUrlPrefix = "type.googleapis.com";
        private const string NameValueSeparator = ": ";
        private const string PropertySeparator = "\n";

        /// <summary>
        /// Returns a formatter using the default settings.
        /// </summary>
        public static TextFormatter Default { get; } = new TextFormatter(Settings.Default);

        // A text formatter which *only* exists
        private static readonly TextFormatter diagnosticFormatter = new TextFormatter(Settings.Default);

        /// <summary>
        /// The text representation of the first 160 characters of Unicode.
        /// Empty strings are replaced by the static constructor.
        /// </summary>
        private static readonly string[] CommonRepresentations = {
            // C0 (ASCII and derivatives) control characters
            "\\u0000", "\\u0001", "\\u0002", "\\u0003",  // 0x00
          "\\u0004", "\\u0005", "\\u0006", "\\u0007",
          "\\b",     "\\t",     "\\n",     "\\u000b",
          "\\f",     "\\r",     "\\u000e", "\\u000f",
          "\\u0010", "\\u0011", "\\u0012", "\\u0013",  // 0x10
          "\\u0014", "\\u0015", "\\u0016", "\\u0017",
          "\\u0018", "\\u0019", "\\u001a", "\\u001b",
          "\\u001c", "\\u001d", "\\u001e", "\\u001f",
            // Escaping of " and \ are required by www.json.org string definition.
            // Escaping of < and > are required for HTML security.
            "", "", "\\\"", "", "",        "", "",        "",  // 0x20
          "", "", "",     "", "",        "", "",        "",
          "", "", "",     "", "",        "", "",        "",  // 0x30
          "", "", "",     "", "\\u003c", "", "\\u003e", "",
          "", "", "",     "", "",        "", "",        "",  // 0x40
          "", "", "",     "", "",        "", "",        "",
          "", "", "",     "", "",        "", "",        "",  // 0x50
          "", "", "",     "", "\\\\",    "", "",        "",
          "", "", "",     "", "",        "", "",        "",  // 0x60
          "", "", "",     "", "",        "", "",        "",
          "", "", "",     "", "",        "", "",        "",  // 0x70
          "", "", "",     "", "",        "", "",        "\\u007f",
            // C1 (ISO 8859 and Unicode) extended control characters
            "\\u0080", "\\u0081", "\\u0082", "\\u0083",  // 0x80
          "\\u0084", "\\u0085", "\\u0086", "\\u0087",
          "\\u0088", "\\u0089", "\\u008a", "\\u008b",
          "\\u008c", "\\u008d", "\\u008e", "\\u008f",
          "\\u0090", "\\u0091", "\\u0092", "\\u0093",  // 0x90
          "\\u0094", "\\u0095", "\\u0096", "\\u0097",
          "\\u0098", "\\u0099", "\\u009a", "\\u009b",
          "\\u009c", "\\u009d", "\\u009e", "\\u009f"
        };

        static TextFormatter()
        {
            for (int i = 0; i < CommonRepresentations.Length; i++)
            {
                if (CommonRepresentations[i] == "")
                {
                    CommonRepresentations[i] = ((char) i).ToString();
                }
            }
        }

        private readonly Settings settings;

        private bool DiagnosticOnly => ReferenceEquals(this, diagnosticFormatter);

        /// <summary>
        /// Creates a new formatted with the given settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public TextFormatter(Settings settings)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Formats the specified message as text.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>The formatted message.</returns>
        public string Format(IMessage message)
        {
            var writer = new StringWriter();
            Format(message, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Formats the specified message as text.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="writer">The TextWriter to write the formatted message to.</param>
        /// <returns>The formatted message.</returns>
        public void Format(IMessage message, TextWriter writer)
        {
            ProtoPreconditions.CheckNotNull(message, nameof(message));
            ProtoPreconditions.CheckNotNull(writer, nameof(writer));

            if (message.Descriptor.IsWellKnownType())
            {
                WriteWellKnownTypeValue(writer, message.Descriptor, message);
            }
            else
            {
                WriteMessage(writer, message);
            }
        }

        /// <summary>
        /// Converts a message to text for diagnostic purposes with no extra context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This differs from calling <see cref="Format(IMessage)"/> on the default text
        /// formatter in its handling of <see cref="Any"/>. As no type registry is available
        /// in <see cref="object.ToString"/> calls, the normal way of resolving the type of
        /// an <c>Any</c> message cannot be applied. Instead, a text property named <c>@value</c>
        /// is included with the base64 data from the <see cref="Any.Value"/> property of the message.
        /// </para>
        /// <para>The value returned by this method is only designed to be used for diagnostic
        /// purposes. It may not be parsable by <see cref="TextParser"/>, and may not be parsable
        /// by other Protocol Buffer implementations.</para>
        /// </remarks>
        /// <param name="message">The message to format for diagnostic purposes.</param>
        /// <returns>The diagnostic-only text representation of the message</returns>
        public static string ToDiagnosticString(IMessage message)
        {
            ProtoPreconditions.CheckNotNull(message, nameof(message));
            return diagnosticFormatter.Format(message);
        }

        private void WriteMessage(TextWriter writer, IMessage message)
        {
            if (message == null)
            {
                WriteNull(writer);
                return;
            }
            if (DiagnosticOnly)
            {
                ICustomDiagnosticMessage customDiagnosticMessage = message as ICustomDiagnosticMessage;
                if (customDiagnosticMessage != null)
                {
                    writer.Write(customDiagnosticMessage.ToDiagnosticString());
                    return;
                }
            }
            
            WriteMessageFields(writer, message, false);
        }

        private bool WriteMessageFields(TextWriter writer, IMessage message, bool assumeFirstFieldWritten)
        {
            var fields = message.Descriptor.Fields;
            bool first = !assumeFirstFieldWritten;
            // First non-oneof fields
            foreach (var field in fields.InFieldNumberOrder())
            {
                var accessor = field.Accessor;

                if (field.ContainingOneof != null && field.ContainingOneof.Accessor.GetCaseFieldDescriptor(message) != field)
                {
                    continue;
                }
                // Omit default values unless we're asked to format them, or they're oneofs (where the default
                // value is still formatted regardless, because that's how we preserve the oneof case).
                object value = accessor.GetValue(message);    

                if (field.ContainingOneof == null && !settings.FormatDefaultValues && IsDefaultValue(accessor, value))
                {
                    continue;
                }

                // Okay, all tests complete: let's write the field value...
                if (!first)
                {
                    writer.Write(PropertySeparator);
                }

                if (field.IsRepeated && !field.IsMap)
                {
                    WriteRepeatedField(writer, field, value);
                    continue;
                }

                WriteSingleField(writer, field, value);

                first = false;
            }
            return !first;
        }
  
        private void WriteSingleField(TextWriter writer, FieldDescriptor field, object value)
        {
            writer.Write(field.Name);

            var wrapped = false;

            if (field.IsMap || (value is IMessage && !(value is Value) && !(value as IMessage).IsValueType()))
            {
                writer.Write(" {\n");
                wrapped = true;
            }
            else
            {
                writer.Write(": ");
            }

            WriteValue(writer, value);

            if (wrapped)
                writer.Write("\n}");
        }

        private void WriteRepeatedField(TextWriter writer, FieldDescriptor field, object value)
        {
            // Repeated field.  Print each element.
            foreach (object element in (IEnumerable) value)
            {
                WriteSingleField(writer, field, element);
                writer.Write(PropertySeparator);
            }
        }

        // Converted from java/core/src/main/java/com/google/protobuf/Descriptors.java
        internal static string ToTextName(string name)
        {
            StringBuilder result = new StringBuilder(name.Length);
            bool isNextUpperCase = false;
            foreach (char ch in name)
            {
                if (ch == '_')
                {
                    isNextUpperCase = true;
                }
                else if (isNextUpperCase)
                {
                    result.Append(char.ToUpperInvariant(ch));
                    isNextUpperCase = false;
                }
                else
                {
                    result.Append(ch);
                }
            }
            return result.ToString();
        }

        internal static string FromJsonName(string name)
        {
            StringBuilder result = new StringBuilder(name.Length);
            foreach (char ch in name)
            {
                if (char.IsUpper(ch))
                {
                    result.Append('_');
                    result.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    result.Append(ch);
                }
            }
            return result.ToString();
        }

        private static void WriteNull(TextWriter writer)
        {
            writer.Write("null");
        }

        private static bool IsDefaultValue(IFieldAccessor accessor, object value)
        {
            if (accessor.Descriptor.IsMap)
            {
                IDictionary dictionary = (IDictionary) value;
                return dictionary.Count == 0;
            }
            if (accessor.Descriptor.IsRepeated)
            {
                IList list = (IList) value;
                return list.Count == 0;
            }
            switch (accessor.Descriptor.FieldType)
            {
                case FieldType.Bool:
                    return (bool) value == false;
                case FieldType.Bytes:
                    return (ByteString) value == ByteString.Empty;
                case FieldType.String:
                    return (string) value == "";
                case FieldType.Double:
                    return (double) value == 0.0;
                case FieldType.SInt32:
                case FieldType.Int32:
                case FieldType.SFixed32:
                case FieldType.Enum:
                    return (int) value == 0;
                case FieldType.Fixed32:
                case FieldType.UInt32:
                    return (uint) value == 0;
                case FieldType.Fixed64:
                case FieldType.UInt64:
                    return (ulong) value == 0;
                case FieldType.SFixed64:
                case FieldType.Int64:
                case FieldType.SInt64:
                    return (long) value == 0;
                case FieldType.Float:
                    return (float) value == 0f;
                case FieldType.Message:
                case FieldType.Group: // Never expect to get this, but...
                    return value == null;
                default:
                    throw new ArgumentException("Invalid field type");
            }
        }

        /// <summary>
        /// Writes a single value to the given writer as text. Only types understood by
        /// Protocol Buffers can be written in this way. This method is only exposed for
        /// advanced use cases; most users should be using <see cref="Format(IMessage)"/>
        /// or <see cref="Format(IMessage, TextWriter)"/>.
        /// </summary>
        /// <param name="writer">The writer to write the value to. Must not be null.</param>
        /// <param name="value">The value to write. May be null.</param>
        public void WriteValue(TextWriter writer, object value)
        {
            if (value == null)
            {
                WriteNull(writer);
            }
            else if (value is bool)
            {
                writer.Write((bool)value ? "true" : "false");
            }
            else if (value is ByteString)
            {
                // Nothing in Base64 needs escaping
                writer.Write('"');
                WriteByteString(writer, (ByteString)value);
                writer.Write('"');
            }
            else if (value is string)
            {
                WriteString(writer, (string)value);
            }
            else if (value is IDictionary)
            {
                WriteDictionary(writer, (IDictionary)value);
            }
            else if (value is IList)
            {
                WriteList(writer, (IList)value);
            }
            else if (value is int || value is uint)
            {
                IFormattable formattable = (IFormattable) value;
                writer.Write(formattable.ToString("d", CultureInfo.InvariantCulture));
            }
            else if (value is long || value is ulong)
            {
                writer.Write('"');
                IFormattable formattable = (IFormattable) value;
                writer.Write(formattable.ToString("d", CultureInfo.InvariantCulture));
                writer.Write('"');
            }
            else if (value is System.Enum)
            {
                if (settings.FormatEnumsAsIntegers)
                {
                    WriteValue(writer, (int)value);
                }
                else
                {
                    string name = OriginalEnumValueHelper.GetOriginalName(value);
                    if (name != null)
                    {
                        WriteString(writer, name);
                    }
                    else
                    {
                        WriteValue(writer, (int)value);
                    }
                }
            }
            else if (value is float || value is double)
            {
                string text = ((IFormattable) value).ToString("r", CultureInfo.InvariantCulture);
                if (text == "NaN" || text == "Infinity" || text == "-Infinity")
                {
                    writer.Write('"');
                    writer.Write(text);
                    writer.Write('"');
                }
                else
                {
                    writer.Write(text);
                }
            }
            else if (value is IMessage)
            {
                Format((IMessage)value, writer);
            }
            else
            {
                throw new ArgumentException("Unable to format value of type " + value.GetType());
            }
        }

        /// <summary>
        /// Central interception point for well-known type formatting. Any well-known types which
        /// don't need special handling can fall back to WriteMessage. We avoid assuming that the
        /// values are using the embedded well-known types, in order to allow for dynamic messages
        /// in the future.
        /// </summary>
        private void WriteWellKnownTypeValue(TextWriter writer, MessageDescriptor descriptor, object value)
        {
            // Currently, we can never actually get here, because null values are always handled by the caller. But if we *could*,
            // this would do the right thing.
            if (value == null)
            {
                WriteNull(writer);
                return;
            }
            // For wrapper types, the value will either be the (possibly boxed) "native" value,
            // or the message itself if we're formatting it at the top level (e.g. just calling ToString on the object itself).
            // If it's the message form, we can extract the value first, which *will* be the (possibly boxed) native value,
            // and then proceed, writing it as if we were definitely in a field. (We never need to wrap it in an extra string...
            // WriteValue will do the right thing.)
            if (descriptor.IsWrapperType())
            {
                if (value is IMessage)
                {
                    var message = (IMessage) value;
                    value = message.Descriptor.Fields[WrappersReflection.WrapperValueFieldNumber].Accessor.GetValue(message);
                }
                WriteValue(writer, value);
                return;
            }
            if (descriptor.FullName == Timestamp.Descriptor.FullName)
            {
                WriteTimestamp(writer, (IMessage)value);
                return;
            }
            if (descriptor.FullName == Duration.Descriptor.FullName)
            {
                WriteDuration(writer, (IMessage)value);
                return;
            }
            if (descriptor.FullName == FieldMask.Descriptor.FullName)
            {
                WriteFieldMask(writer, (IMessage)value);
                return;
            }
            if (descriptor.FullName == Struct.Descriptor.FullName)
            {
                WriteStruct(writer, (IMessage)value);
                return;
            }
            if (descriptor.FullName == ListValue.Descriptor.FullName)
            {
                var fieldAccessor = descriptor.Fields[ListValue.ValuesFieldNumber].Accessor;
                WriteList(writer, (IList)fieldAccessor.GetValue((IMessage)value));
                return;
            }
            if (descriptor.FullName == Value.Descriptor.FullName)
            {
                WriteStructFieldValue(writer, (IMessage)value);
                return;
            }
            if (descriptor.FullName == Any.Descriptor.FullName)
            {
                WriteAny(writer, (IMessage)value);
                return;
            }
            WriteMessage(writer, (IMessage)value);
        }

        private void WriteTimestamp(TextWriter writer, IMessage value)
        {
            // TODO: In the common case where this *is* using the built-in Timestamp type, we could
            // avoid all the reflection at this point, by casting to Timestamp. In the interests of
            // avoiding subtle bugs, don't do that until we've implemented DynamicMessage so that we can prove
            // it still works in that case.
            int nanos = (int) value.Descriptor.Fields[Timestamp.NanosFieldNumber].Accessor.GetValue(value);
            long seconds = (long) value.Descriptor.Fields[Timestamp.SecondsFieldNumber].Accessor.GetValue(value);
            writer.Write(ProtobufAdapter.TimestampToText(seconds, nanos, DiagnosticOnly));
        }

        private void WriteDuration(TextWriter writer, IMessage value)
        {
            // TODO: Same as for WriteTimestamp
            int nanos = (int) value.Descriptor.Fields[Duration.NanosFieldNumber].Accessor.GetValue(value);
            long seconds = (long) value.Descriptor.Fields[Duration.SecondsFieldNumber].Accessor.GetValue(value);
            writer.Write(ProtobufAdapter.DurationToText(seconds, nanos, DiagnosticOnly));
        }

        private void WriteFieldMask(TextWriter writer, IMessage value)
        {
            var paths = (IList<string>) value.Descriptor.Fields[FieldMask.PathsFieldNumber].Accessor.GetValue(value);
            writer.Write(ProtobufAdapter.FieldMaskToText(paths, DiagnosticOnly));
        }

        private void WriteAny(TextWriter writer, IMessage value)
        {
            if (DiagnosticOnly)
            {
                WriteDiagnosticOnlyAny(writer, value);
                return;
            }

            string typeUrl = (string) value.Descriptor.Fields[Any.TypeUrlFieldNumber].Accessor.GetValue(value);
            ByteString data = (ByteString) value.Descriptor.Fields[Any.ValueFieldNumber].Accessor.GetValue(value);
            string typeName = Any.GetTypeName(typeUrl);
            MessageDescriptor descriptor = settings.TypeRegistry.Find(typeName);
            if (descriptor == null)
            {
                throw new InvalidOperationException($"Type registry has no descriptor for type name '{typeName}'");
            }
            IMessage message = descriptor.Parser.ParseFrom(data);
            writer.Write("{ ");
            WriteString(writer, AnyTypeUrlField);
            writer.Write(NameValueSeparator);
            WriteString(writer, typeUrl);

            if (descriptor.IsWellKnownType())
            {
                writer.Write(PropertySeparator);
                WriteString(writer, AnyWellKnownTypeValueField);
                writer.Write(NameValueSeparator);
                WriteWellKnownTypeValue(writer, descriptor, message);
            }
            else
            {
                WriteMessageFields(writer, message, true);
            }
            writer.Write(" }");
        }

        private void WriteDiagnosticOnlyAny(TextWriter writer, IMessage value)
        {
            string typeUrl = (string) value.Descriptor.Fields[Any.TypeUrlFieldNumber].Accessor.GetValue(value);
            ByteString data = (ByteString) value.Descriptor.Fields[Any.ValueFieldNumber].Accessor.GetValue(value);
            writer.Write("{ ");
            WriteString(writer, AnyTypeUrlField);
            writer.Write(NameValueSeparator);
            WriteString(writer, typeUrl);
            writer.Write(PropertySeparator);
            WriteString(writer, AnyDiagnosticValueField);
            writer.Write(NameValueSeparator);
            writer.Write('"');
            writer.Write(data.ToBase64());
            writer.Write('"');
            writer.Write(" }");
        }

        private void WriteStruct(TextWriter writer, IMessage message)
        {
            writer.Write("{ ");
            IDictionary fields = (IDictionary) message.Descriptor.Fields[Struct.FieldsFieldNumber].Accessor.GetValue(message);
            bool first = true;
            foreach (DictionaryEntry entry in fields)
            {
                string key = (string) entry.Key;
                IMessage value = (IMessage) entry.Value;
                if (string.IsNullOrEmpty(key) || value == null)
                {
                    throw new InvalidOperationException("Struct fields cannot have an empty key or a null value.");
                }

                if (!first)
                {
                    writer.Write(PropertySeparator);
                }
                WriteString(writer, key);
                writer.Write(NameValueSeparator);
                WriteStructFieldValue(writer, value);
                first = false;
            }
            writer.Write(first ? "}" : " }");
        }

        private void WriteStructFieldValue(TextWriter writer, IMessage message)
        {
            var specifiedField = message.Descriptor.Oneofs[0].Accessor.GetCaseFieldDescriptor(message);
            if (specifiedField == null)
            {
                throw new InvalidOperationException("Value message must contain a value for the oneof.");
            }

            object value = specifiedField.Accessor.GetValue(message);

            switch (specifiedField.FieldNumber)
            {
                case Value.BoolValueFieldNumber:
                case Value.StringValueFieldNumber:
                case Value.NumberValueFieldNumber:
                    WriteValue(writer, value);
                    return;
                case Value.StructValueFieldNumber:
                case Value.ListValueFieldNumber:
                    // Structs and ListValues are nested messages, and already well-known types.
                    var nestedMessage = (IMessage) specifiedField.Accessor.GetValue(message);
                    WriteWellKnownTypeValue(writer, nestedMessage.Descriptor, nestedMessage);
                    return;
                case Value.NullValueFieldNumber:
                    WriteNull(writer);
                    return;
                default:
                    throw new InvalidOperationException("Unexpected case in struct field: " + specifiedField.FieldNumber);
            }
        }

        internal void WriteList(TextWriter writer, IList list)
        {
            writer.Write("[ ");
            bool first = true;
            foreach (var value in list)
            {
                if (!first)
                {
                    writer.Write(PropertySeparator);
                }
                WriteValue(writer, value);
                first = false;
            }
            writer.Write(first ? "]" : " ]");
        }

        internal void WriteDictionary(TextWriter writer, IDictionary dictionary)
        {
            bool first = true;
            // This will box each pair. Could use IDictionaryEnumerator, but that's ugly in terms of disposal.
            foreach (DictionaryEntry pair in dictionary)
            {
                if (!first)
                {
                    writer.Write(PropertySeparator);
                }
                string keyText;
                if (pair.Key is string)
                {
                    keyText = (string) pair.Key;
                }
                else if (pair.Key is bool)
                {
                    keyText = (bool) pair.Key ? "true" : "false";
                }
                else if (pair.Key is int || pair.Key is uint | pair.Key is long || pair.Key is ulong)
                {
                    keyText = ((IFormattable) pair.Key).ToString("d", CultureInfo.InvariantCulture);
                }
                else
                {
                    if (pair.Key == null)
                    {
                        throw new ArgumentException("Dictionary has entry with null key");
                    }
                    throw new ArgumentException("Unhandled dictionary key type: " + pair.Key.GetType());
                }
                WriteString(writer, keyText);
                writer.Write(NameValueSeparator);
                WriteValue(writer, pair.Value);
                first = false;
            }
        }

        /// <summary>
        /// Writes a string (including leading and trailing double quotes) to a builder, escaping as required.
        /// </summary>
        /// <remarks>
        /// Other than surrogate pair handling, this code is mostly taken from src/google/protobuf/util/internal/json_escaping.cc.
        /// </remarks>
        internal static void WriteString(TextWriter writer, string text)
        {
            writer.Write('"');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < 0xa0)
                {
                    writer.Write(CommonRepresentations[c]);
                    continue;
                }
                if (char.IsHighSurrogate(c))
                {
                    // Encountered first part of a surrogate pair.
                    // Check that we have the whole pair, and encode both parts as hex.
                    i++;
                    if (i == text.Length || !char.IsLowSurrogate(text[i]))
                    {
                        throw new ArgumentException("String contains low surrogate not followed by high surrogate");
                    }
                    HexEncodeUtf16CodeUnit(writer, c);
                    HexEncodeUtf16CodeUnit(writer, text[i]);
                    continue;
                }
                else if (char.IsLowSurrogate(c))
                {
                    throw new ArgumentException("String contains high surrogate not preceded by low surrogate");
                }
                switch ((uint) c)
                {
                    // These are not required by json spec
                    // but used to prevent security bugs in javascript.
                    case 0xfeff:  // Zero width no-break space
                    case 0xfff9:  // Interlinear annotation anchor
                    case 0xfffa:  // Interlinear annotation separator
                    case 0xfffb:  // Interlinear annotation terminator

                    case 0x00ad:  // Soft-hyphen
                    case 0x06dd:  // Arabic end of ayah
                    case 0x070f:  // Syriac abbreviation mark
                    case 0x17b4:  // Khmer vowel inherent Aq
                    case 0x17b5:  // Khmer vowel inherent Aa
                        HexEncodeUtf16CodeUnit(writer, c);
                        break;

                    default:
                        if ((c >= 0x0600 && c <= 0x0603) ||  // Arabic signs
                            (c >= 0x200b && c <= 0x200f) ||  // Zero width etc.
                            (c >= 0x2028 && c <= 0x202e) ||  // Separators etc.
                            (c >= 0x2060 && c <= 0x2064) ||  // Invisible etc.
                            (c >= 0x206a && c <= 0x206f))
                        {
                            HexEncodeUtf16CodeUnit(writer, c);
                        }
                        else
                        {
                            // No handling of surrogates here - that's done earlier
                            writer.Write(c);
                        }
                        break;
                }
            }
            writer.Write('"');
        }

        private const string Hex = "0123456789abcdef";
        private static void HexEncodeUtf16CodeUnit(TextWriter writer, char c)
        {
            writer.Write("\\u");
            writer.Write(Hex[(c >> 12) & 0xf]);
            writer.Write(Hex[(c >> 8) & 0xf]);
            writer.Write(Hex[(c >> 4) & 0xf]);
            writer.Write(Hex[(c >> 0) & 0xf]);
        }

        public static string EncodeByteString(ByteString byteString)
        {
            var writer = new StringWriter();
            WriteByteString(writer, byteString);
            return writer.GetStringBuilder().ToString();
        }

        /// <summary>
        /// Escapes bytes in the format used in protocol buffer text format, which
        /// is the same as the format used for C string literals.  All bytes
        /// that are not printable 7-bit ASCII characters are escaped, as well as
        /// backslash, single-quote, and double-quote characters.  Characters for
        /// which no defined short-hand escape sequence is defined will be escaped
        /// using 3-digit octal sequences.
        /// The returned value is guaranteed to be entirely ASCII.
        /// </summary>
        static void WriteByteString(TextWriter writer, ByteString input)
        {
            foreach (byte b in input)
            {
                switch (b)
                {
                        // C# does not use \a or \v
                    case 0x07:
                        writer.Write("\\a");
                        break;
                    case (byte) '\b':
                        writer.Write("\\b");
                        break;
                    case (byte) '\f':
                        writer.Write("\\f");
                        break;
                    case (byte) '\n':
                        writer.Write("\\n");
                        break;
                    case (byte) '\r':
                        writer.Write("\\r");
                        break;
                    case (byte) '\t':
                        writer.Write("\\t");
                        break;
                    case 0x0b:
                        writer.Write("\\v");
                        break;
                    case (byte) '\\':
                        writer.Write("\\\\");
                        break;
                    case (byte) '\'':
                        writer.Write("\\\'");
                        break;
                    case (byte) '"':
                        writer.Write("\\\"");
                        break;
                    default:
                        if (b >= 0x20 && b < 128)
                        {
                            writer.Write((char) b);
                        }
                        else
                        {
                            writer.Write('\\');
                            writer.Write((char) ('0' + ((b >> 6) & 3)));
                            writer.Write((char) ('0' + ((b >> 3) & 7)));
                            writer.Write((char) ('0' + (b & 7)));
                        }
                        break;
                }
            }
        }

        internal static ByteString DecodeBytes(string input)
        {
            byte[] result = new byte[input.Length];
            int pos = 0;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c != '\\')
                {
                    result[pos++] = (byte) c;
                    continue;
                }
                if (i + 1 >= input.Length)
                {
                    throw new FormatException("Invalid escape sequence: '\\' at end of string.");
                }

                i++;
                c = input[i];
                if (c >= '0' && c <= '7')
                {
                    // Octal escape. 
                    int code = ParseDigit(c);
                    if (i + 1 < input.Length && IsOctal(input[i + 1]))
                    {
                        i++;
                        code = code*8 + ParseDigit(input[i]);
                    }
                    if (i + 1 < input.Length && IsOctal(input[i + 1]))
                    {
                        i++;
                        code = code*8 + ParseDigit(input[i]);
                    }
                    result[pos++] = (byte) code;
                }
                else
                {
                    switch (c)
                    {
                        case 'a':
                            result[pos++] = 0x07;
                            break;
                        case 'b':
                            result[pos++] = (byte) '\b';
                            break;
                        case 'f':
                            result[pos++] = (byte) '\f';
                            break;
                        case 'n':
                            result[pos++] = (byte) '\n';
                            break;
                        case 'r':
                            result[pos++] = (byte) '\r';
                            break;
                        case 't':
                            result[pos++] = (byte) '\t';
                            break;
                        case 'v':
                            result[pos++] = 0x0b;
                            break;
                        case '\\':
                            result[pos++] = (byte) '\\';
                            break;
                        case '\'':
                            result[pos++] = (byte) '\'';
                            break;
                        case '"':
                            result[pos++] = (byte) '\"';
                            break;

                        case 'x':
                            // hex escape
                            int code;
                            if (i + 1 < input.Length && IsHex(input[i + 1]))
                            {
                                i++;
                                code = ParseDigit(input[i]);
                            }
                            else
                            {
                                throw new FormatException("Invalid escape sequence: '\\x' with no digits");
                            }
                            if (i + 1 < input.Length && IsHex(input[i + 1]))
                            {
                                ++i;
                                code = code*16 + ParseDigit(input[i]);
                            }
                            result[pos++] = (byte) code;
                            break;

                        default:
                            throw new FormatException("Invalid escape sequence: '\\" + c + "'");
                    }
                }
            }

            return ByteString.CopyFrom(result, 0, pos);
        }

        /// <summary>
        /// Interprets a character as a digit (in any base up to 36) and returns the
        /// numeric value.
        /// </summary>
        private static int ParseDigit(char c)
        {
            if ('0' <= c && c <= '9')
            {
                return c - '0';
            }
            else if ('a' <= c && c <= 'z')
            {
                return c - 'a' + 10;
            }
            else
            {
                return c - 'A' + 10;
            }
        }

        /// <summary>
        /// Tests a character to see if it's an octal digit.
        /// </summary>
        private static bool IsOctal(char c)
        {
            return '0' <= c && c <= '7';
        }

        /// <summary>
        /// Tests a character to see if it's a hex digit.
        /// </summary>
        private static bool IsHex(char c)
        {
            return ('0' <= c && c <= '9') ||
                   ('a' <= c && c <= 'f') ||
                   ('A' <= c && c <= 'F');
        }

        /// <summary>
        /// Settings controlling text formatting.
        /// </summary>
        public sealed class Settings
        {
            /// <summary>
            /// Default settings, as used by <see cref="TextFormatter.Default"/>
            /// </summary>
            public static Settings Default { get; }

            // Workaround for the Mono compiler complaining about XML comments not being on
            // valid language elements.
            static Settings()
            {
                Default = new Settings(false, TypeRegistry.Empty, true);
            }

            /// <summary>
            /// Whether fields whose values are the default for the field type (e.g. 0 for integers)
            /// should be formatted (true) or omitted (false).
            /// </summary>
            public bool FormatDefaultValues { get; }

            /// <summary>
            /// The type registry used to format <see cref="Any"/> messages.
            /// </summary>
            public TypeRegistry TypeRegistry { get; }

            /// <summary>
            /// Whether to format enums as ints. Defaults to false.
            /// </summary>
            public bool FormatEnumsAsIntegers { get; }


            /// <summary>
            /// Creates a new <see cref="Settings"/> object with the specified formatting of default values
            /// and an empty type registry.
            /// </summary>
            /// <param name="formatDefaultValues"><c>true</c> if default values (0, empty strings etc) should be formatted; <c>false</c> otherwise.</param>
            public Settings(bool formatDefaultValues) : this(formatDefaultValues, TypeRegistry.Empty)
            {
            }

            /// <summary>
            /// Creates a new <see cref="Settings"/> object with the specified formatting of default values
            /// and type registry.
            /// </summary>
            /// <param name="formatDefaultValues"><c>true</c> if default values (0, empty strings etc) should be formatted; <c>false</c> otherwise.</param>
            /// <param name="typeRegistry">The <see cref="TypeRegistry"/> to use when formatting <see cref="Any"/> messages.</param>
            public Settings(bool formatDefaultValues, TypeRegistry typeRegistry) : this(formatDefaultValues, typeRegistry, false)
            {
            }

            /// <summary>
            /// Creates a new <see cref="Settings"/> object with the specified parameters.
            /// </summary>
            /// <param name="formatDefaultValues"><c>true</c> if default values (0, empty strings etc) should be formatted; <c>false</c> otherwise.</param>
            /// <param name="typeRegistry">The <see cref="TypeRegistry"/> to use when formatting <see cref="Any"/> messages. TypeRegistry.Empty will be used if it is null.</param>
            /// <param name="formatEnumsAsIntegers"><c>true</c> to format the enums as integers; <c>false</c> to format enums as enum names.</param>
            private Settings(bool formatDefaultValues,
                            TypeRegistry typeRegistry,
                            bool formatEnumsAsIntegers)
            {
                FormatDefaultValues = formatDefaultValues;
                TypeRegistry = typeRegistry ?? TypeRegistry.Empty;
                FormatEnumsAsIntegers = formatEnumsAsIntegers;
            }

            /// <summary>
            /// Creates a new <see cref="Settings"/> object with the specified formatting of default values and the current settings.
            /// </summary>
            /// <param name="formatDefaultValues"><c>true</c> if default values (0, empty strings etc) should be formatted; <c>false</c> otherwise.</param>
            public Settings WithFormatDefaultValues(bool formatDefaultValues) => new Settings(formatDefaultValues, TypeRegistry, FormatEnumsAsIntegers);

            /// <summary>
            /// Creates a new <see cref="Settings"/> object with the specified type registry and the current settings.
            /// </summary>
            /// <param name="typeRegistry">The <see cref="TypeRegistry"/> to use when formatting <see cref="Any"/> messages.</param>
            public Settings WithTypeRegistry(TypeRegistry typeRegistry) => new Settings(FormatDefaultValues, typeRegistry, FormatEnumsAsIntegers);

            /// <summary>
            /// Creates a new <see cref="Settings"/> object with the specified enums formatting option and the current settings.
            /// </summary>
            /// <param name="formatEnumsAsIntegers"><c>true</c> to format the enums as integers; <c>false</c> to format enums as enum names.</param>
            public Settings WithFormatEnumsAsIntegers(bool formatEnumsAsIntegers) => new Settings(FormatDefaultValues, TypeRegistry, formatEnumsAsIntegers);
        }

    }
}