#region Copyright notice and license
// Protocol Buffers - Google's data interchange format
// Copyright 2008 Google Inc.  All rights reserved.
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Protobuf.Text
{
    
    /// <summary>
    /// Provides ASCII text formatting support for messages.
    /// TODO(jonskeet): Support for alternative line endings.
    /// (Easy to print, via TextGenerator. Not sure about parsing.)
    /// </summary>
    internal static class TextFormatter
    {
        /// <summary>
        /// Format message to string
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string Format(IMessage message)
        {
            var writer = new StringWriter();
            Print(message, writer);
            return writer.ToString();
        }

        /// <summary>
        /// Outputs a textual representation of the Protocol Message supplied into
        /// the parameter output.
        /// </summary>
        public static void Print(IMessage message, TextWriter output)
        {
            TextGenerator generator = new TextGenerator(output, "\n");
            Print(message, generator);
        }


        public static string PrintToString(IMessage message)
        {
            StringWriter text = new StringWriter();
            Print(message, text);
            return text.ToString();
        }


        private static void Print(IMessage message, TextGenerator generator)
        {
            foreach (var field in message.Descriptor.Fields.InFieldNumberOrder())
            {
                var accessor = field.Accessor;
                if (field.ContainingOneof != null && field.ContainingOneof.Accessor.GetCaseFieldDescriptor(message) != field)
                {
                    continue;
                }
                // Omit default values unless we're asked to format them, or they're oneofs (where the default
                // value is still formatted regardless, because that's how we preserve the oneof case).
                object value = accessor.GetValue(message);

                if (value == null)
                    continue;

                PrintField(field, value, generator);                
            }
        }

        internal static void PrintField(FieldDescriptor field, object value, TextGenerator generator)
        {
            if (field.IsRepeated)
            {
                // Repeated field.  Print each element.
                foreach (object element in (IEnumerable) value)
                {
                    PrintSingleField(field, element, generator);
                }
            }
            else
            {
                PrintSingleField(field, value, generator);
            }
        }

        private static void PrintSingleField(FieldDescriptor field, Object value, TextGenerator generator)
        {
            if (field.IsExtension())
            {
                generator.Print("[");
                // We special-case MessageSet elements for compatibility with proto1.
                if (//field.ContainingType.Options.MessageSetWireFormat &&
                    field.FieldType == FieldType.Message
                    && !field.IsRequired
                    // object equality (TODO(jonskeet): Work out what this comment means!)
                    && field.ExtendeeType == field.MessageType)
                {
                    generator.Print(field.MessageType.FullName);
                }
                else
                {
                    generator.Print(field.FullName);
                }

                generator.Print("]");
            }
            else
            {
                if (field.FieldType == FieldType.Group)
                {
                    // Groups must be serialized with their original capitalization.
                    generator.Print(field.MessageType.Name);
                }
                else
                {
                    generator.Print(field.Name);
                }
            }

            if (field.IsMap && field.FieldType == FieldType.Message)
            {
                generator.Print(" {\n");
                generator.Indent();
            }
            else
            {
                generator.Print(": ");
            }

            PrintFieldValue(value, generator);

            if (field.IsMap && field.FieldType == FieldType.Message)
            {
                generator.Outdent();
                generator.Print("}");
            }

            generator.Print("\n");
        }

        private static void PrintFieldValue(object value, TextGenerator generator)
        {
            if (value == null)
            {
            }
            else if (value is bool)
            {
                generator.Print((bool)value ? "true" : "false");
            }
            else if (value is ByteString)
            {
                generator.Print("\"");
                generator.Print(EscapeBytes((ByteString)value));
                generator.Print("\"");
            }
            else if (value is string)
            {
                generator.Print("\"");
                generator.Print(EscapeText((string)value));
                generator.Print("\"");
            }
            else if (value is IConvertible)
            {
                generator.Print(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
            }
            else if (value is Enum)
            {
                generator.Print(((EnumValueDescriptor)value).Name);
            }
            else if (value is float || value is double)
            {
                string text = ((IFormattable)value).ToString("r", CultureInfo.InvariantCulture);
                if (text == "NaN" || text == "Infinity" || text == "-Infinity")
                {
                    generator.Print("\"");
                    generator.Print(text);
                    generator.Print("\"");
                }
                else
                {
                    generator.Print(text);
                }
            }
            else if (value is IMessage)
            {
                Print((IMessage)value, generator);
            }
            else
            {
                throw new ArgumentException("Unable to format value of type " + value.GetType());
            }
        }

        private static void PrintFieldValue(FieldDescriptor field, object value, TextGenerator generator)
        {
            switch (field.FieldType)
            {
                    // The Float and Double types must specify the "r" format to preserve their precision, otherwise,
                    // the double to/from string will trim the precision to 6 places.  As with other numeric formats
                    // below, always use the invariant culture so it's predictable.
                case FieldType.Float:
                    generator.Print(((float)value).ToString("r", CultureInfo.InvariantCulture));
                    break;
                case FieldType.Double:
                    generator.Print(((double)value).ToString("r", CultureInfo.InvariantCulture));
                    break;

                case FieldType.Int32:
                case FieldType.Int64:
                case FieldType.SInt32:
                case FieldType.SInt64:
                case FieldType.SFixed32:
                case FieldType.SFixed64:
                case FieldType.UInt32:
                case FieldType.UInt64:
                case FieldType.Fixed32:
                case FieldType.Fixed64:
                    // The simple Object.ToString converts using the current culture.
                    // We want to always use the invariant culture so it's predictable.
                    generator.Print(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    break;
                case FieldType.Bool:
                    // Explicitly use the Java true/false
                    generator.Print((bool) value ? "true" : "false");
                    break;

                case FieldType.String:
                    generator.Print("\"");
                    generator.Print(EscapeText((string) value));
                    generator.Print("\"");
                    break;

                case FieldType.Bytes:
                    {
                        generator.Print("\"");
                        generator.Print(EscapeBytes((ByteString) value));
                        generator.Print("\"");
                        break;
                    }

                case FieldType.Enum:
                    {
                        /*
                        if (value is IEnumLite && !(value is EnumValueDescriptor))
                        {
                            throw new NotSupportedException("Lite enumerations are not supported.");
                        }
                        */
                        generator.Print(((EnumValueDescriptor) value).Name);
                        break;
                    }

                case FieldType.Message:
                case FieldType.Group:
                    /*
                    if (value is IMessageLite && !(value is IMessage))
                    {
                        throw new NotSupportedException("Lite messages are not supported.");
                    }
                     */
                    Print((IMessage) value, generator);
                    break;
            }
        }

        /// <summary>
        /// Like <see cref="EscapeBytes" /> but escapes a text string.
        /// The string is first encoded as UTF-8, then each byte escaped individually.
        /// The returned value is guaranteed to be entirely ASCII.
        /// </summary>
        public static string EscapeText(string input)
        {
            return EscapeBytes(ByteString.CopyFromUtf8(input));
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
        public static String EscapeBytes(ByteString input)
        {
            StringBuilder builder = new StringBuilder(input.Length);
            foreach (byte b in input)
            {
                switch (b)
                {
                        // C# does not use \a or \v
                    case 0x07:
                        builder.Append("\\a");
                        break;
                    case (byte) '\b':
                        builder.Append("\\b");
                        break;
                    case (byte) '\f':
                        builder.Append("\\f");
                        break;
                    case (byte) '\n':
                        builder.Append("\\n");
                        break;
                    case (byte) '\r':
                        builder.Append("\\r");
                        break;
                    case (byte) '\t':
                        builder.Append("\\t");
                        break;
                    case 0x0b:
                        builder.Append("\\v");
                        break;
                    case (byte) '\\':
                        builder.Append("\\\\");
                        break;
                    case (byte) '\'':
                        builder.Append("\\\'");
                        break;
                    case (byte) '"':
                        builder.Append("\\\"");
                        break;
                    default:
                        if (b >= 0x20 && b < 128)
                        {
                            builder.Append((char) b);
                        }
                        else
                        {
                            builder.Append('\\');
                            builder.Append((char) ('0' + ((b >> 6) & 3)));
                            builder.Append((char) ('0' + ((b >> 3) & 7)));
                            builder.Append((char) ('0' + (b & 7)));
                        }
                        break;
                }
            }
            return builder.ToString();
        }        

        /*

        [CLSCompliant(false)]
        public static ulong ParseUInt64(string text)
        {
            return (ulong) ParseInteger(text, false, true);
        }

        public static long ParseInt64(string text)
        {
            return ParseInteger(text, true, true);
        }

        [CLSCompliant(false)]
        public static uint ParseUInt32(string text)
        {
            return (uint) ParseInteger(text, false, false);
        }

        public static int ParseInt32(string text)
        {
            return (int) ParseInteger(text, true, false);
        }

        public static float ParseFloat(string text)
        {
            switch (text)
            {
                case "-inf":
                case "-infinity":
                case "-inff":
                case "-infinityf":
                    return float.NegativeInfinity;
                case "inf":
                case "infinity":
                case "inff":
                case "infinityf":
                    return float.PositiveInfinity;
                case "nan":
                case "nanf":
                    return float.NaN;
                default:
                    return float.Parse(text, FrameworkPortability.InvariantCulture);
            }
        }

        public static double ParseDouble(string text)
        {
            switch (text)
            {
                case "-inf":
                case "-infinity":
                    return double.NegativeInfinity;
                case "inf":
                case "infinity":
                    return double.PositiveInfinity;
                case "nan":
                    return double.NaN;
                default:
                    return double.Parse(text, FrameworkPortability.InvariantCulture);
            }
        }

        /// <summary>
        /// Parses an integer in hex (leading 0x), decimal (no prefix) or octal (leading 0).
        /// Only a negative sign is permitted, and it must come before the radix indicator.
        /// </summary>
        private static long ParseInteger(string text, bool isSigned, bool isLong)
        {
            string original = text;
            bool negative = false;
            if (text.StartsWith("-"))
            {
                if (!isSigned)
                {
                    throw new FormatException("Number must be positive: " + original);
                }
                negative = true;
                text = text.Substring(1);
            }

            int radix = 10;
            if (text.StartsWith("0x"))
            {
                radix = 16;
                text = text.Substring(2);
            }
            else if (text.StartsWith("0"))
            {
                radix = 8;
            }

            ulong result;
            try
            {
                // Workaround for https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=278448
                // We should be able to use Convert.ToUInt64 for all cases.
                result = radix == 10 ? ulong.Parse(text) : Convert.ToUInt64(text, radix);
            }
            catch (OverflowException)
            {
                // Convert OverflowException to FormatException so there's a single exception type this method can throw.
                string numberDescription = string.Format("{0}-bit {1}signed integer", isLong ? 64 : 32,
                                                         isSigned ? "" : "un");
                throw new FormatException("Number out of range for " + numberDescription + ": " + original);
            }

            if (negative)
            {
                ulong max = isLong ? 0x8000000000000000UL : 0x80000000L;
                if (result > max)
                {
                    string numberDescription = string.Format("{0}-bit signed integer", isLong ? 64 : 32);
                    throw new FormatException("Number out of range for " + numberDescription + ": " + original);
                }
                return -((long) result);
            }
            else
            {
                ulong max = isSigned
                                ? (isLong ? (ulong) long.MaxValue : int.MaxValue)
                                : (isLong ? ulong.MaxValue : uint.MaxValue);
                if (result > max)
                {
                    string numberDescription = string.Format("{0}-bit {1}signed integer", isLong ? 64 : 32,
                                                             isSigned ? "" : "un");
                    throw new FormatException("Number out of range for " + numberDescription + ": " + original);
                }
                return (long) result;
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

        

        public static void Merge(string text, IBuilder builder)
        {
            Merge(text, ExtensionRegistry.Empty, builder);
        }

        public static void Merge(TextReader reader, IBuilder builder)
        {
            Merge(reader, ExtensionRegistry.Empty, builder);
        }

        public static void Merge(TextReader reader, ExtensionRegistry registry, IBuilder builder)
        {
            Merge(reader.ReadToEnd(), registry, builder);
        }

        public static void Merge(string text, ExtensionRegistry registry, IBuilder builder)
        {
            TextTokenizer tokenizer = new TextTokenizer(text);

            while (!tokenizer.AtEnd)
            {
                MergeField(tokenizer, registry, builder);
            }
        }

        /// <summary>
        /// Parses a single field from the specified tokenizer and merges it into
        /// the builder.
        /// </summary>
        private static void MergeField(TextTokenizer tokenizer, ExtensionRegistry extensionRegistry,
                                       IBuilder builder)
        {
            FieldDescriptor field;
            MessageDescriptor type = builder.DescriptorForType;
            ExtensionInfo extension = null;

            if (tokenizer.TryConsume("["))
            {
                // An extension.
                StringBuilder name = new StringBuilder(tokenizer.ConsumeIdentifier());
                while (tokenizer.TryConsume("."))
                {
                    name.Append(".");
                    name.Append(tokenizer.ConsumeIdentifier());
                }

                extension = extensionRegistry.FindByName(type, name.ToString());

                if (extension == null)
                {
                    throw tokenizer.CreateFormatExceptionPreviousToken("Extension \"" + name +
                                                                       "\" not found in the ExtensionRegistry.");
                }
                else if (extension.Descriptor.ContainingType != type)
                {
                    throw tokenizer.CreateFormatExceptionPreviousToken("Extension \"" + name +
                                                                       "\" does not extend message type \"" +
                                                                       type.FullName + "\".");
                }

                tokenizer.Consume("]");

                field = extension.Descriptor;
            }
            else
            {
                String name = tokenizer.ConsumeIdentifier();
                field = type.FindDescriptor<FieldDescriptor>(name);

                // Group names are expected to be capitalized as they appear in the
                // .proto file, which actually matches their type names, not their field
                // names.
                if (field == null)
                {
                    // Explicitly specify the invariant culture so that this code does not break when
                    // executing in Turkey.
#if PORTABLE_LIBRARY
                    String lowerName = name.ToLowerInvariant();
#else
                    String lowerName = name.ToLower(FrameworkPortability.InvariantCulture);
#endif
                    field = type.FindDescriptor<FieldDescriptor>(lowerName);
                    // If the case-insensitive match worked but the field is NOT a group,
                    // TODO(jonskeet): What? Java comment ends here!
                    if (field != null && field.FieldType != FieldType.Group)
                    {
                        field = null;
                    }
                }
                // Again, special-case group names as described above.
                if (field != null && field.FieldType == FieldType.Group && field.MessageType.Name != name)
                {
                    field = null;
                }

                if (field == null)
                {
                    throw tokenizer.CreateFormatExceptionPreviousToken(
                        "Message type \"" + type.FullName + "\" has no field named \"" + name + "\".");
                }
            }

            object value = null;

            if (field.MappedType == MappedType.Message)
            {
                tokenizer.TryConsume(":"); // optional

                String endToken;
                if (tokenizer.TryConsume("<"))
                {
                    endToken = ">";
                }
                else
                {
                    tokenizer.Consume("{");
                    endToken = "}";
                }

                IBuilder subBuilder;
                
                if (extension == null)
                {
                    subBuilder = builder.CreateBuilderForField(field);
                }
                else
                {
                    subBuilder = extension.DefaultInstance.WeakCreateBuilderForType() as IBuilder;
                    if (subBuilder == null)
                    {
                        throw new NotSupportedException("Lite messages are not supported.");
                    }
                }

                while (!tokenizer.TryConsume(endToken))
                {
                    if (tokenizer.AtEnd)
                    {
                        throw tokenizer.CreateFormatException("Expected \"" + endToken + "\".");
                    }
                    MergeField(tokenizer, extensionRegistry, subBuilder);
                }

                value = subBuilder.WeakBuild();
            }
            else
            {
                tokenizer.Consume(":");

                switch (field.FieldType)
                {
                    case FieldType.Int32:
                    case FieldType.SInt32:
                    case FieldType.SFixed32:
                        value = tokenizer.ConsumeInt32();
                        break;

                    case FieldType.Int64:
                    case FieldType.SInt64:
                    case FieldType.SFixed64:
                        value = tokenizer.ConsumeInt64();
                        break;

                    case FieldType.UInt32:
                    case FieldType.Fixed32:
                        value = tokenizer.ConsumeUInt32();
                        break;

                    case FieldType.UInt64:
                    case FieldType.Fixed64:
                        value = tokenizer.ConsumeUInt64();
                        break;

                    case FieldType.Float:
                        value = tokenizer.ConsumeFloat();
                        break;

                    case FieldType.Double:
                        value = tokenizer.ConsumeDouble();
                        break;

                    case FieldType.Bool:
                        value = tokenizer.ConsumeBoolean();
                        break;

                    case FieldType.String:
                        value = tokenizer.ConsumeString();
                        break;

                    case FieldType.Bytes:
                        value = tokenizer.ConsumeByteString();
                        break;

                    case FieldType.Enum:
                        {
                            EnumDescriptor enumType = field.EnumType;

                            if (tokenizer.LookingAtInteger())
                            {
                                int number = tokenizer.ConsumeInt32();
                                value = enumType.FindValueByNumber(number);
                                if (value == null)
                                {
                                    throw tokenizer.CreateFormatExceptionPreviousToken(
                                        "Enum type \"" + enumType.FullName +
                                        "\" has no value with number " + number + ".");
                                }
                            }
                            else
                            {
                                String id = tokenizer.ConsumeIdentifier();
                                value = enumType.FindValueByName(id);
                                if (value == null)
                                {
                                    throw tokenizer.CreateFormatExceptionPreviousToken(
                                        "Enum type \"" + enumType.FullName +
                                        "\" has no value named \"" + id + "\".");
                                }
                            }

                            break;
                        }

                    case FieldType.Message:
                    case FieldType.Group:
                        throw new InvalidOperationException("Can't get here.");
                }
            }

            if (field.IsRepeated)
            {
                builder.WeakAddRepeatedField(field, value);
            }
            else
            {
                builder.SetField(field, value);
            }
        }
        */
    }


    class TextGenerator
    {
        /// <summary>
        /// The string to use at the end of each line. We assume that "Print" is only called using \n
        /// to indicate a line break; that's what we use to detect when we need to indent etc, and
        /// *just* the \n is replaced with the contents of lineBreak.
        /// </summary>
        private readonly string lineBreak;

        /// <summary>
        /// Writer to write formatted text to.
        /// </summary>
        private readonly TextWriter writer;

        /// <summary>
        /// Keeps track of whether the next piece of text should be indented
        /// </summary>
        private bool atStartOfLine = true;

        /// <summary>
        /// Keeps track of the current level of indentation
        /// </summary>
        private readonly StringBuilder indent = new StringBuilder();

        /// <summary>
        /// Creates a generator writing to the given writer. The writer
        /// is not closed by this class.
        /// </summary>
        public TextGenerator(TextWriter writer, string lineBreak)
        {
            this.writer = writer;
            this.lineBreak = lineBreak;
        }

        /// <summary>
        /// Indents text by two spaces. After calling Indent(), two spaces
        /// will be inserted at the beginning of each line of text. Indent() may
        /// be called multiple times to produce deeper indents.
        /// </summary>
        public void Indent()
        {
            indent.Append("  ");
        }

        /// <summary>
        /// Reduces the current indent level by two spaces.
        /// </summary>
        public void Outdent()
        {
            if (indent.Length == 0)
            {
                throw new InvalidOperationException("Too many calls to Outdent()");
            }
            indent.Length -= 2;
        }

        public void WriteLine(string text)
        {
            Print(text);
            Print("\n");
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public void WriteLine()
        {
            WriteLine("");
        }

        /// <summary>
        /// Prints the given text to the output stream, indenting at line boundaries.
        /// </summary>
        /// <param name="text"></param>
        public void Print(string text)
        {
            int pos = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    // Strip off the \n from what we write
                    Write(text.Substring(pos, i - pos));
                    Write(lineBreak);
                    pos = i + 1;
                    atStartOfLine = true;
                }
            }
            Write(text.Substring(pos));
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        private void Write(string data)
        {
            if (data.Length == 0)
            {
                return;
            }
            if (atStartOfLine)
            {
                atStartOfLine = false;
                writer.Write(indent);
            }
            writer.Write(data);
        }
    } 
}