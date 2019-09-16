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

namespace Protobuf.Text
{
    internal sealed class TextToken : IEquatable<TextToken>
    {
        // Tokens with no value can be reused.
        private static readonly TextToken _true = new TextToken(TokenType.True);
        private static readonly TextToken _false = new TextToken(TokenType.False);
        private static readonly TextToken _null = new TextToken(TokenType.Null);
        private static readonly TextToken startObject = new TextToken(TokenType.StartObject);
        private static readonly TextToken endObject = new TextToken(TokenType.EndObject);
        private static readonly TextToken startArray = new TextToken(TokenType.StartArray);
        private static readonly TextToken endArray = new TextToken(TokenType.EndArray);
        private static readonly TextToken endDocument = new TextToken(TokenType.EndDocument);

        internal static TextToken Null { get { return _null; } }
        internal static TextToken False { get { return _false; } }
        internal static TextToken True { get { return _true; } }
        internal static TextToken StartObject{ get { return startObject; } }
        internal static TextToken EndObject { get { return endObject; } }
        internal static TextToken StartArray { get { return startArray; } }
        internal static TextToken EndArray { get { return endArray; } }
        internal static TextToken EndDocument { get { return endDocument; } }

        internal static TextToken Name(string name)
        {
            return new TextToken(TokenType.Name, stringValue: name);
        }

        internal static TextToken Value(string value)
        {
            return new TextToken(TokenType.StringValue, stringValue: value);
        }

        internal static TextToken Value(double value)
        {
            return new TextToken(TokenType.Number, numberValue: value);
        }

        // A value is a string, number, array, object, null, true or false
        // Arrays and objects have start/end
        // A document consists of a value
        // Objects are name/value sequences.

        private readonly TokenType type;
        private readonly string stringValue;
        private readonly double numberValue;

        internal TokenType Type { get { return type; } }
        internal string StringValue { get { return stringValue; } }
        internal double NumberValue { get { return numberValue; } }

        private TextToken(TokenType type, string stringValue = null, double numberValue = 0)
        {
            this.type = type;
            this.stringValue = stringValue;
            this.numberValue = numberValue;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TextToken);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int) type;
                hash = hash * 31 + stringValue == null ? 0 : stringValue.GetHashCode();
                hash = hash * 31 + numberValue.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            switch (type)
            {
                case TokenType.Null:
                    return "null";
                case TokenType.True:
                    return "true";
                case TokenType.False:
                    return "false";
                case TokenType.Name:
                    return "name (" + stringValue + ")";
                case TokenType.StringValue:
                    return "value (" + stringValue + ")";
                case TokenType.Number:
                    return "number (" + numberValue + ")";
                case TokenType.StartObject:
                    return "start-object";
                case TokenType.EndObject:
                    return "end-object";
                case TokenType.StartArray:
                    return "start-array";
                case TokenType.EndArray:
                    return "end-array";
                case TokenType.EndDocument:
                    return "end-document";
                default:
                    throw new InvalidOperationException("Token is of unknown type " + type);
            }
        }

        public bool Equals(TextToken other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            // Note use of other.numberValue.Equals rather than ==, so that NaN compares appropriately.
            return other.type == type && other.stringValue == stringValue && other.numberValue.Equals(numberValue);
        }
    }
}