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

using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.TestProtos;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using Xunit.Abstractions;
using Protobuf.Text;
using System;
using System.Globalization;

namespace Test
{
    /// <summary>
    /// Unit tests for JSON parsing.
    /// </summary>
    public class TextParserTest
    {
        // Sanity smoke test
        [Fact]
        public void AllTypesRoundtrip()
        {
            AssertRoundtrip(SampleMessages.CreateFullTestAllTypes());
        }

        [Fact]
        public void Maps()
        {
            AssertRoundtrip(new TestMap { MapStringString = { { "with spaces", "bar" }, { "a", "b" } } });
            AssertRoundtrip(new TestMap { MapInt32Int32 = { { 0, 1 }, { 2, 3 } } });
            AssertRoundtrip(new TestMap { MapBoolBool = { { false, true }, { true, false } } });
        }

        [Theory]
        [InlineData(" 1 ")]
        [InlineData("+1")]
        [InlineData("1,000")]
        [InlineData("1.5")]
        public void IntegerMapKeysAreStrict(string keyText)
        {
            // Test that integer parsing is strict. We assume that if this is correct for int32,
            // it's correct for other numeric key types.
            var json = "mapInt32Int32: {" + keyText + " : \"1\" }";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TextParser.Default.Parse<TestMap>(json));
        }

        [Fact]
        public void OriginalFieldNameAccepted()
        {
            var json = "single_int32: 10";
            var expected = new TestAllTypes { SingleInt32 = 10 };
            Assert.Equal(expected, TestAllTypes.Parser.ParseText(json));
        }

        [Fact]
        public void SourceContextRoundtrip()
        {
            AssertRoundtrip(new SourceContext { FileName = "foo.proto" });
        }

        [Fact]
        public void SingularWrappers_DefaultNonNullValues()
        {
            var message = new TestWellKnownTypes
            {
                StringField = "",
                BytesField = ByteString.Empty,
                BoolField = false,
                FloatField = 0f,
                DoubleField = 0d,
                Int32Field = 0,
                Int64Field = 0,
                Uint32Field = 0,
                Uint64Field = 0
            };

            AssertRoundtrip(message);
        }

        [Fact]
        public void SingularWrappers_NonDefaultValues()
        {
            var message = new TestWellKnownTypes
            {
                StringField = "x",
                BytesField = ByteString.CopyFrom(1, 2, 3),
                BoolField = true,
                FloatField = 12.5f,
                DoubleField = 12.25d,
                Int32Field = 1,
                Int64Field = 2,
                Uint32Field = 3,
                Uint64Field = 4
            };

            AssertRoundtrip(message);
        }

        [Fact]
        public void SingularWrappers_ExplicitNulls()
        {
            // When we parse the "valueField": null part, we remember it... basically, it's one case
            // where explicit default values don't fully roundtrip.
            var message = new TestWellKnownTypes { ValueField = Value.ForNull() };
            var json = new JsonFormatter(new JsonFormatter.Settings(true)).Format(message);
            var parsed = TextParser.Default.Parse<TestWellKnownTypes>(json);
            Assert.Equal(message, parsed);
        }

        [Theory]
        [InlineData(typeof(BoolValue), "true", true)]
        [InlineData(typeof(Int32Value), "32", 32)]
        [InlineData(typeof(Int64Value), "32", 32L)]
        [InlineData(typeof(Int64Value), "\"32\"", 32L)]
        [InlineData(typeof(UInt32Value), "32", 32U)]
        [InlineData(typeof(UInt64Value), "\"32\"", 32UL)]
        [InlineData(typeof(UInt64Value), "32", 32UL)]
        [InlineData(typeof(StringValue), "\"foo\"", "foo")]
        [InlineData(typeof(FloatValue), "1.5", 1.5f)]
        [InlineData(typeof(DoubleValue), "1.5", 1.5d)]

        public void Wrappers_Standalone(System.Type wrapperType, string json, object expectedValue)
        {
            IMessage parsed = (IMessage)Activator.CreateInstance(wrapperType);
            IMessage expected = (IMessage)Activator.CreateInstance(wrapperType);
            TextParser.Default.Merge(parsed, "null");
            Assert.Equal(expected, parsed);

            TextParser.Default.Merge(parsed, json);
            expected.Descriptor.Fields[Protobuf.Text.WrappersReflection.WrapperValueFieldNumber].Accessor.SetValue(expected, expectedValue);
            Assert.Equal(expected, parsed);
        }

        [Fact]
        public void ExplicitNullValue()
        {
            string json = "valueField: null";
            var message = TextParser.Default.Parse<TestWellKnownTypes>(json);
            Assert.Equal(new TestWellKnownTypes { ValueField = Value.ForNull() }, message);
        }

        [Fact]
        public void BytesWrapper_Standalone()
        {
            ByteString data = ByteString.CopyFrom(1, 2, 3);
            // Can't do this with attributes...
            var parsed = TextParser.Default.Parse<BytesValue>(WrapInQuotes(data.ToBase64()));
            var expected = new BytesValue { Value = data };
            Assert.Equal(expected, parsed);
        }

        [Fact]
        public void RepeatedWrappers()
        {
            var message = new RepeatedWellKnownTypes
            {
                BoolField = { true, false },
                BytesField = { ByteString.CopyFrom(1, 2, 3), ByteString.CopyFrom(4, 5, 6), ByteString.Empty },
                DoubleField = { 12.5, -1.5, 0d },
                FloatField = { 123.25f, -20f, 0f },
                Int32Field = { int.MaxValue, int.MinValue, 0 },
                Int64Field = { long.MaxValue, long.MinValue, 0L },
                StringField = { "First", "Second", "" },
                Uint32Field = { uint.MaxValue, uint.MinValue, 0U },
                Uint64Field = { ulong.MaxValue, ulong.MinValue, 0UL },
            };
            AssertRoundtrip(message);
        }

        [Fact]
        public void RepeatedField_NullElementProhibited()
        {
            string json = "{ \"repeated_foreign_message\": [null] }";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Fact]
        public void RepeatedField_NullOverallValueAllowed()
        {
            string json = "repeated_foreign_message: null";
            Assert.Equal(new TestAllTypes(), TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("mapInt32Int32: { 10: null }")]
        [InlineData("mapStringString: { abc: null }")]
        [InlineData("mapInt32ForeignMessage: { 10: null }")]
        public void MapField_NullValueProhibited(string json)
        {
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestMap.Parser.ParseText(json));
        }

        [Fact]
        public void MapField_NullOverallValueAllowed()
        {
            string json = "mapInt32Int32: null";
            Assert.Equal(new TestMap(), TestMap.Parser.ParseText(json));
        }

        [Fact]
        public void IndividualWrapperTypes()
        {
            Assert.Equal(new StringValue { Value = "foo" }, StringValue.Parser.ParseText("\"foo\""));
            Assert.Equal(new Int32Value { Value = 1 }, Int32Value.Parser.ParseText("1"));
            // Can parse strings directly too
            Assert.Equal(new Int32Value { Value = 1 }, Int32Value.Parser.ParseText("\"1\""));
        }

        private static void AssertRoundtrip<T>(T message) where T : IMessage<T>, new()
        {
            var clone = message.Clone();
            var json = TextFormatter.Format(message);
            var parsed = TextParser.Default.Parse<T>(json);
            Assert.Equal(clone, parsed);
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("-0", 0)] // Not entirely clear whether we intend to allow this...
        [InlineData("1", 1)]
        [InlineData("-1", -1)]
        [InlineData("2147483647", 2147483647)]
        [InlineData("-2147483648", -2147483648)]
        public void StringToInt32_Valid(string jsonValue, int expectedParsedValue)
        {
            string json = "singleInt32: \"" + jsonValue + "\"";
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleInt32);
        }

        [Theory]
        [InlineData("+0")]
        [InlineData(" 1")]
        [InlineData("1 ")]
        [InlineData("00")]
        [InlineData("-00")]
        [InlineData("--1")]
        [InlineData("+1")]
        [InlineData("1.5")]
        [InlineData("1e10")]
        [InlineData("2147483648")]
        [InlineData("-2147483649")]
        public void StringToInt32_Invalid(string jsonValue)
        {
            string json = "singleInt32: \"" + jsonValue + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0U)]
        [InlineData("1", 1U)]
        [InlineData("4294967295", 4294967295U)]
        public void StringToUInt32_Valid(string jsonValue, uint expectedParsedValue)
        {
            string json = "singleUint32: \"" + jsonValue + "\"";
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleUint32);
        }

        // Assume that anything non-bounds-related is covered in the Int32 case
        [Theory]
        [InlineData("-1")]
        [InlineData("4294967296")]
        public void StringToUInt32_Invalid(string jsonValue)
        {
            string json = "singleUint32: \"" + jsonValue + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0L)]
        [InlineData("1", 1L)]
        [InlineData("-1", -1L)]
        [InlineData("9223372036854775807", 9223372036854775807)]
        [InlineData("-9223372036854775808", -9223372036854775808)]
        public void StringToInt64_Valid(string jsonValue, long expectedParsedValue)
        {
            string json = "singleInt64: \"" + jsonValue + "\"";
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleInt64);
        }

        // Assume that anything non-bounds-related is covered in the Int32 case
        [Theory]
        [InlineData("-9223372036854775809")]
        [InlineData("9223372036854775808")]
        public void StringToInt64_Invalid(string jsonValue)
        {
            string json = "singleInt64: \"" + jsonValue + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0UL)]
        [InlineData("1", 1UL)]
        [InlineData("18446744073709551615", 18446744073709551615)]
        public void StringToUInt64_Valid(string jsonValue, ulong expectedParsedValue)
        {
            string json = "singleUint64: \"" + jsonValue + "\"";
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleUint64);
        }

        // Assume that anything non-bounds-related is covered in the Int32 case
        [Theory]
        [InlineData("-1")]
        [InlineData("18446744073709551616")]
        public void StringToUInt64_Invalid(string jsonValue)
        {
            string json = "singleUint64: \"" + jsonValue + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0d)]
        [InlineData("1", 1d)]
        [InlineData("1.000000", 1d)]
        [InlineData("1.0000000000000000000000001", 1d)] // We don't notice that we haven't preserved the exact value
        [InlineData("-1", -1d)]
        [InlineData("1e1", 10d)]
        [InlineData("1e01", 10d)] // Leading decimals are allowed in exponents
        [InlineData("1E1", 10d)] // Either case is fine
        [InlineData("-1e1", -10d)]
        [InlineData("1.5e1", 15d)]
        [InlineData("-1.5e1", -15d)]
        [InlineData("15e-1", 1.5d)]
        [InlineData("-15e-1", -1.5d)]
        [InlineData("1.79769e308", 1.79769e308)]
        [InlineData("-1.79769e308", -1.79769e308)]
        [InlineData("Infinity", double.PositiveInfinity)]
        [InlineData("-Infinity", double.NegativeInfinity)]
        [InlineData("NaN", double.NaN)]
        public void StringToDouble_Valid(string jsonValue, double expectedParsedValue)
        {
            string json = "singleDouble: \"" + jsonValue + "\"";
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleDouble);
        }

        [Theory]
        [InlineData("1.7977e308")]
        [InlineData("-1.7977e308")]
        [InlineData("1e309")]
        [InlineData("1,0")]
        [InlineData("1.0.0")]
        [InlineData("+1")]
        [InlineData("00")]
        [InlineData("01")]
        [InlineData("-00")]
        [InlineData("-01")]
        [InlineData("--1")]
        [InlineData(" Infinity")]
        [InlineData(" -Infinity")]
        [InlineData("NaN ")]
        [InlineData("Infinity ")]
        [InlineData("-Infinity ")]
        [InlineData(" NaN")]
        [InlineData("INFINITY")]
        [InlineData("nan")]
        [InlineData("\u00BD")] // 1/2 as a single Unicode character. Just sanity checking...
        public void StringToDouble_Invalid(string jsonValue)
        {
            string json = "singleDouble: \"" + jsonValue + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0f)]
        [InlineData("1", 1f)]
        [InlineData("1.000000", 1f)]
        [InlineData("-1", -1f)]
        [InlineData("3.402823e38", 3.402823e38f)]
        [InlineData("-3.402823e38", -3.402823e38f)]
        [InlineData("1.5e1", 15f)]
        [InlineData("15e-1", 1.5f)]
        public void StringToFloat_Valid(string jsonValue, float expectedParsedValue)
        {
            string json = "singleFloat: \"" + jsonValue + "\"";
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleFloat);
        }

        [Theory]
        [InlineData("3.402824e38")]
        [InlineData("-3.402824e38")]
        [InlineData("1,0")]
        [InlineData("1.0.0")]
        [InlineData("+1")]
        [InlineData("00")]
        [InlineData("--1")]
        public void StringToFloat_Invalid(string jsonValue)
        {
            string json = "singleFloat: \"" + jsonValue + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("-0", 0)] // Not entirely clear whether we intend to allow this...
        [InlineData("1", 1)]
        [InlineData("-1", -1)]
        [InlineData("2147483647", 2147483647)]
        [InlineData("-2147483648", -2147483648)]
        [InlineData("1e1", 10)]
        [InlineData("-1e1", -10)]
        [InlineData("10.00", 10)]
        [InlineData("-10.00", -10)]
        public void NumberToInt32_Valid(string jsonValue, int expectedParsedValue)
        {
            string json = "singleInt32: " + jsonValue;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleInt32);
        }

        [Theory]
        [InlineData("+0", typeof(InvalidJsonException))]
        [InlineData("00", typeof(InvalidJsonException))]
        [InlineData("-00", typeof(InvalidJsonException))]
        [InlineData("--1", typeof(InvalidJsonException))]
        [InlineData("+1", typeof(InvalidJsonException))]
        [InlineData("1.5", typeof(InvalidTextProtocolBufferException))]
        // Value is out of range
        [InlineData("1e10", typeof(InvalidTextProtocolBufferException))]
        [InlineData("2147483648", typeof(InvalidTextProtocolBufferException))]
        [InlineData("-2147483649", typeof(InvalidTextProtocolBufferException))]
        public void NumberToInt32_Invalid(string jsonValue, System.Type expectedExceptionType)
        {
            string json = "singleInt32: " + jsonValue;
            Assert.Throws(expectedExceptionType, () => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0U)]
        [InlineData("1", 1U)]
        [InlineData("4294967295", 4294967295U)]
        public void NumberToUInt32_Valid(string jsonValue, uint expectedParsedValue)
        {
            string json = "singleUint32: " + jsonValue;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleUint32);
        }

        // Assume that anything non-bounds-related is covered in the Int32 case
        [Theory]
        [InlineData("-1")]
        [InlineData("4294967296")]
        public void NumberToUInt32_Invalid(string jsonValue)
        {
            string json = "singleUint32: " + jsonValue;
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0L)]
        [InlineData("1", 1L)]
        [InlineData("-1", -1L)]
        // long.MaxValue isn't actually representable as a double. This string value is the highest
        // representable value which isn't greater than long.MaxValue.
        [InlineData("9223372036854774784", 9223372036854774784)]
        [InlineData("-9223372036854775808", -9223372036854775808)]
        public void NumberToInt64_Valid(string jsonValue, long expectedParsedValue)
        {
            string json = "singleInt64: " + jsonValue;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleInt64);
        }

        // Assume that anything non-bounds-related is covered in the Int32 case
        [Theory]
        [InlineData("9223372036854775808")]
        // Theoretical bound would be -9223372036854775809, but when that is parsed to a double
        // we end up with the exact value of long.MinValue due to lack of precision. The value here
        // is the "next double down".
        [InlineData("-9223372036854780000")]
        public void NumberToInt64_Invalid(string jsonValue)
        {
            string json = "singleInt64: " + jsonValue;
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0UL)]
        [InlineData("1", 1UL)]
        // ulong.MaxValue isn't representable as a double. This value is the largest double within
        // the range of ulong.
        [InlineData("18446744073709549568", 18446744073709549568UL)]
        public void NumberToUInt64_Valid(string jsonValue, ulong expectedParsedValue)
        {
            string json = "singleUint64: " + jsonValue;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleUint64);
        }

        // Assume that anything non-bounds-related is covered in the Int32 case
        [Theory]
        [InlineData("-1")]
        [InlineData("18446744073709551616")]
        public void NumberToUInt64_Invalid(string jsonValue)
        {
            string json = "singleUint64: " + jsonValue;
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0d)]
        [InlineData("1", 1d)]
        [InlineData("1.000000", 1d)]
        [InlineData("1.0000000000000000000000001", 1d)] // We don't notice that we haven't preserved the exact value
        [InlineData("-1", -1d)]
        [InlineData("1e1", 10d)]
        [InlineData("1e01", 10d)] // Leading decimals are allowed in exponents
        [InlineData("1E1", 10d)] // Either case is fine
        [InlineData("-1e1", -10d)]
        [InlineData("1.5e1", 15d)]
        [InlineData("-1.5e1", -15d)]
        [InlineData("15e-1", 1.5d)]
        [InlineData("-15e-1", -1.5d)]
        [InlineData("1.79769e308", 1.79769e308)]
        [InlineData("-1.79769e308", -1.79769e308)]
        public void NumberToDouble_Valid(string jsonValue, double expectedParsedValue)
        {
            string json = "singleDouble: " + jsonValue;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleDouble);
        }

        [Theory]
        [InlineData("1.7977e308")]
        [InlineData("-1.7977e308")]
        [InlineData("1e309")]
        [InlineData("1,0")]
        [InlineData("1.0.0")]
        [InlineData("+1")]
        [InlineData("00")]
        [InlineData("--1")]
        [InlineData("\u00BD")] // 1/2 as a single Unicode character. Just sanity checking...
        public void NumberToDouble_Invalid(string jsonValue)
        {
            string json = "singleDouble: " + jsonValue;
            Assert.Throws<InvalidJsonException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("0", 0f)]
        [InlineData("1", 1f)]
        [InlineData("1.000000", 1f)]
        [InlineData("-1", -1f)]
        [InlineData("3.402823e38", 3.402823e38f)]
        [InlineData("-3.402823e38", -3.402823e38f)]
        [InlineData("1.5e1", 15f)]
        [InlineData("15e-1", 1.5f)]
        public void NumberToFloat_Valid(string jsonValue, float expectedParsedValue)
        {
            string json = "singleFloat: " + jsonValue;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(expectedParsedValue, parsed.SingleFloat);
        }

        [Theory]
        [InlineData("3.402824e38", typeof(InvalidTextProtocolBufferException))]
        [InlineData("-3.402824e38", typeof(InvalidTextProtocolBufferException))]
        [InlineData("1,0", typeof(InvalidJsonException))]
        [InlineData("1.0.0", typeof(InvalidJsonException))]
        [InlineData("+1", typeof(InvalidJsonException))]
        [InlineData("00", typeof(InvalidJsonException))]
        [InlineData("--1", typeof(InvalidJsonException))]
        public void NumberToFloat_Invalid(string jsonValue, System.Type expectedExceptionType)
        {
            string json = "singleFloat: " + jsonValue;
            Assert.Throws(expectedExceptionType, () => TestAllTypes.Parser.ParseText(json));
        }

        // The simplest way of testing that the value has parsed correctly is to reformat it,
        // as we trust the formatting. In many cases that will give the same result as the input,
        // so in those cases we accept an expectedFormatted value of null. Sometimes the results
        // will be different though, due to a different number of digits being provided.
        [Theory]
        // Z offset
        [InlineData("2015-10-09T14:46:23.123456789Z", null)]
        [InlineData("2015-10-09T14:46:23.123456Z", null)]
        [InlineData("2015-10-09T14:46:23.123Z", null)]
        [InlineData("2015-10-09T14:46:23Z", null)]
        [InlineData("2015-10-09T14:46:23.123456000Z", "2015-10-09T14:46:23.123456Z")]
        [InlineData("2015-10-09T14:46:23.1234560Z", "2015-10-09T14:46:23.123456Z")]
        [InlineData("2015-10-09T14:46:23.123000000Z", "2015-10-09T14:46:23.123Z")]
        [InlineData("2015-10-09T14:46:23.1230Z", "2015-10-09T14:46:23.123Z")]
        [InlineData("2015-10-09T14:46:23.00Z", "2015-10-09T14:46:23Z")]

        // +00:00 offset
        [InlineData("2015-10-09T14:46:23.123456789+00:00", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T14:46:23.123456+00:00", "2015-10-09T14:46:23.123456Z")]
        [InlineData("2015-10-09T14:46:23.123+00:00", "2015-10-09T14:46:23.123Z")]
        [InlineData("2015-10-09T14:46:23+00:00", "2015-10-09T14:46:23Z")]
        [InlineData("2015-10-09T14:46:23.123456000+00:00", "2015-10-09T14:46:23.123456Z")]
        [InlineData("2015-10-09T14:46:23.1234560+00:00", "2015-10-09T14:46:23.123456Z")]
        [InlineData("2015-10-09T14:46:23.123000000+00:00", "2015-10-09T14:46:23.123Z")]
        [InlineData("2015-10-09T14:46:23.1230+00:00", "2015-10-09T14:46:23.123Z")]
        [InlineData("2015-10-09T14:46:23.00+00:00", "2015-10-09T14:46:23Z")]

        // Other offsets (assume by now that the subsecond handling is okay)
        [InlineData("2015-10-09T15:46:23.123456789+01:00", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T13:46:23.123456789-01:00", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T15:16:23.123456789+00:30", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T14:16:23.123456789-00:30", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T16:31:23.123456789+01:45", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T13:01:23.123456789-01:45", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-10T08:46:23.123456789+18:00", "2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-08T20:46:23.123456789-18:00", "2015-10-09T14:46:23.123456789Z")]

        // Leap years and min/max
        [InlineData("2016-02-29T14:46:23.123456789Z", null)]
        [InlineData("2000-02-29T14:46:23.123456789Z", null)]
        [InlineData("0001-01-01T00:00:00Z", null)]
        [InlineData("9999-12-31T23:59:59.999999999Z", null)]
        public void Timestamp_Valid(string jsonValue, string expectedFormatted)
        {
            expectedFormatted = expectedFormatted ?? jsonValue;
            string json = WrapInQuotes(jsonValue);
            var parsed = Timestamp.Parser.ParseText(json);
            Assert.Equal(WrapInQuotes(expectedFormatted), parsed.ToString());
        }

        [Theory]
        [InlineData("2015-10-09 14:46:23.123456789Z")]
        [InlineData("2015/10/09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T14.46.23.123456789Z")]
        [InlineData("2015-10-09T14:46:23,123456789Z")]
        [InlineData(" 2015-10-09T14:46:23.123456789Z")]
        [InlineData("2015-10-09T14:46:23.123456789Z ")]
        [InlineData("2015-10-09T14:46:23.1234567890")]
        [InlineData("2015-10-09T14:46:23.123456789")]
        [InlineData("2015-13-09T14:46:23.123456789Z")]
        [InlineData("2015-10-32T14:46:23.123456789Z")]
        [InlineData("2015-10-09T24:00:00.000000000Z")]
        [InlineData("2015-10-09T14:60:23.123456789Z")]
        [InlineData("2015-10-09T14:46:60.123456789Z")]
        [InlineData("2015-10-09T14:46:23.123456789+18:01")]
        [InlineData("2015-10-09T14:46:23.123456789-18:01")]
        [InlineData("2015-10-09T14:46:23.123456789-00:00")]
        [InlineData("0001-01-01T00:00:00+00:01")]
        [InlineData("9999-12-31T23:59:59.999999999-00:01")]
        [InlineData("2100-02-29T14:46:23.123456789Z")]
        public void Timestamp_Invalid(string jsonValue)
        {
            string json = WrapInQuotes(jsonValue);
            Assert.Throws<InvalidTextProtocolBufferException>(() => Timestamp.Parser.ParseText(json));
        }

        [Fact]
        public void StructValue_Null()
        {
            Assert.Equal(new Value { NullValue = 0 }, Value.Parser.ParseText("null"));
        }

        [Fact]
        public void StructValue_String()
        {
            Assert.Equal(new Value { StringValue = "hi" }, Value.Parser.ParseText("\"hi\""));
        }

        [Fact]
        public void StructValue_Bool()
        {
            Assert.Equal(new Value { BoolValue = true }, Value.Parser.ParseText("true"));
            Assert.Equal(new Value { BoolValue = false }, Value.Parser.ParseText("false"));
        }

        [Fact]
        public void StructValue_List()
        {
            Assert.Equal(Value.ForList(Value.ForNumber(1), Value.ForString("x")), Value.Parser.ParseText("[1, \"x\"]"));
        }

        [Fact]
        public void Value_List_WithNullElement()
        {
            var expected = Value.ForList(Value.ForString("x"), Value.ForNull(), Value.ForString("y"));
            var actual = Value.Parser.ParseText("[\"x\", null, \"y\"]");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void StructValue_NullElement()
        {
            var expected = Value.ForStruct(new Struct { Fields = { { "x", Value.ForNull() } } });
            var actual = Value.Parser.ParseText("x: null");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseListValue()
        {
            Assert.Equal(new ListValue { Values = { Value.ForNumber(1), Value.ForString("x") } }, ListValue.Parser.ParseText("[1, \"x\"]"));
        }

        [Fact]
        public void StructValue_Struct()
        {
            Assert.Equal(
                Value.ForStruct(new Struct { Fields = { { "x", Value.ForNumber(1) }, { "y", Value.ForString("z") } } }),
                Value.Parser.ParseText("x: 1, y: \"z\""));
        }

        [Fact]
        public void ParseStruct()
        {
            Assert.Equal(new Struct { Fields = { { "x", Value.ForNumber(1) }, { "y", Value.ForString("z") } } },
                Struct.Parser.ParseText("x: 1, y: \"z\""));
        }

        // TODO for duration parsing: upper and lower bounds.
        // +/- 315576000000 seconds

        [Theory]
        [InlineData("1.123456789s", null)]
        [InlineData("1.123456s", null)]
        [InlineData("1.123s", null)]
        [InlineData("1.12300s", "1.123s")]
        [InlineData("1.12345s", "1.123450s")]
        [InlineData("1s", null)]
        [InlineData("-1.123456789s", null)]
        [InlineData("-1.123456s", null)]
        [InlineData("-1.123s", null)]
        [InlineData("-1s", null)]
        [InlineData("0.123s", null)]
        [InlineData("-0.123s", null)]
        [InlineData("123456.123s", null)]
        [InlineData("-123456.123s", null)]
        // Upper and lower bounds
        [InlineData("315576000000s", null)]
        [InlineData("-315576000000s", null)]
        public void Duration_Valid(string jsonValue, string expectedFormatted)
        {
            expectedFormatted = expectedFormatted ?? jsonValue;
            string json = WrapInQuotes(jsonValue);
            var parsed = Duration.Parser.ParseText(json);
            Assert.Equal(WrapInQuotes(expectedFormatted), parsed.ToString());
        }

        // The simplest way of testing that the value has parsed correctly is to reformat it,
        // as we trust the formatting. In many cases that will give the same result as the input,
        // so in those cases we accept an expectedFormatted value of null. Sometimes the results
        // will be different though, due to a different number of digits being provided.
        [Theory]
        [InlineData("1.1234567890s")]
        [InlineData("1.123456789")]
        [InlineData("1.123456789ss")]
        [InlineData("1.123456789S")]
        [InlineData("+1.123456789s")]
        [InlineData(".123456789s")]
        [InlineData("1,123456789s")]
        [InlineData("1x1.123456789s")]
        [InlineData("1.1x3456789s")]
        [InlineData(" 1.123456789s")]
        [InlineData("1.123456789s ")]
        [InlineData("01.123456789s")]
        [InlineData("-01.123456789s")]
        [InlineData("--0.123456789s")]
        // Violate upper/lower bounds in various ways
        [InlineData("315576000001s")]
        [InlineData("3155760000000s")]
        [InlineData("-3155760000000s")]
        public void Duration_Invalid(string jsonValue)
        {
            string json = WrapInQuotes(jsonValue);
            Assert.Throws<InvalidTextProtocolBufferException>(() => Duration.Parser.ParseText(json));
        }

        // Not as many tests for field masks as I'd like; more to be added when we have more
        // detailed specifications.

        [Theory]
        [InlineData("")]
        [InlineData("foo", "foo")]
        [InlineData("foo,bar", "foo", "bar")]
        [InlineData("foo.bar", "foo.bar")]
        [InlineData("fooBar", "foo_bar")]
        [InlineData("fooBar.bazQux", "foo_bar.baz_qux")]
        public void FieldMask_Valid(string jsonValue, params string[] expectedPaths)
        {
            string json = WrapInQuotes(jsonValue);
            var parsed = FieldMask.Parser.ParseText(json);
            Assert.Equal(expectedPaths, parsed.Paths);
        }

        [Theory]
        [InlineData("foo_bar")]
        public void FieldMask_Invalid(string jsonValue)
        {
            string json = WrapInQuotes(jsonValue);
            Assert.Throws<InvalidTextProtocolBufferException>(() => FieldMask.Parser.ParseText(json));
        }

        [Fact]
        public void Any_RegularMessage()
        {
            var registry = TypeRegistry.FromMessages(TestAllTypes.Descriptor);
            var formatter = new JsonFormatter(new JsonFormatter.Settings(false, TypeRegistry.FromMessages(TestAllTypes.Descriptor)));
            var message = new TestAllTypes { SingleInt32 = 10, SingleNestedMessage = new TestAllTypes.Types.NestedMessage { Bb = 20 } };
            var original = Any.Pack(message);
            var json = formatter.Format(original); // This is tested in JsonFormatterTest
            var parser = new TextParser(new TextParser.Settings(10, registry));
            Assert.Equal(original, parser.Parse<Any>(json));
            string valueFirstJson = "singleInt32: 10\nsingleNestedMessage: { bb: 20 }\n@type: \"type.googleapis.com/protobuf_unittest3.TestAllTypes\"";
            Assert.Equal(original, parser.Parse<Any>(valueFirstJson));
        }

        [Fact]
        public void Any_CustomPrefix()
        {
            var registry = TypeRegistry.FromMessages(TestAllTypes.Descriptor);
            var message = new TestAllTypes { SingleInt32 = 10 };
            var original = Any.Pack(message, "custom.prefix/middle-part");
            var parser = new TextParser(new TextParser.Settings(10, registry));
            string json = "@type: \"custom.prefix/middle-part/protobuf_unittest3.TestAllTypes\"\nsingleInt32\n: 10";
            Assert.Equal(original, parser.Parse<Any>(json));
        }

        [Fact]
        public void Any_UnknownType()
        {
            string json = "@type: \"type.googleapis.com/bogus\"";
            Assert.Throws<InvalidOperationException>(() => Any.Parser.ParseText(json));
        }

        [Fact]
        public void Any_NoTypeUrl()
        {
            string json = "foo: \"bar\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => Any.Parser.ParseText(json));
        }

        [Fact]
        public void Any_WellKnownType()
        {
            var registry = TypeRegistry.FromMessages(Timestamp.Descriptor);
            var formatter = new JsonFormatter(new JsonFormatter.Settings(false, registry));
            var timestamp = new DateTime(1673, 6, 19, 12, 34, 56, DateTimeKind.Utc).ToTimestamp();
            var original = Any.Pack(timestamp);
            var json = formatter.Format(original); // This is tested in JsonFormatterTest
            var parser = new TextParser(new TextParser.Settings(10, registry));
            Assert.Equal(original, parser.Parse<Any>(json));
            string valueFirstJson = "value: \"1673-06-19T12:34:56Z\"\n@type: \"type.googleapis.com/google.protobuf.Timestamp\"";
            Assert.Equal(original, parser.Parse<Any>(valueFirstJson));
        }

        [Fact]
        public void Any_Nested()
        {
            var registry = TypeRegistry.FromMessages(TestWellKnownTypes.Descriptor, TestAllTypes.Descriptor);
            var formatter = new JsonFormatter(new JsonFormatter.Settings(false, registry));
            var parser = new TextParser(new TextParser.Settings(10, registry));
            var doubleNestedMessage = new TestAllTypes { SingleInt32 = 20 };
            var nestedMessage = Any.Pack(doubleNestedMessage);
            var message = new TestWellKnownTypes { AnyField = Any.Pack(nestedMessage) };
            var json = formatter.Format(message);
            // Use the descriptor-based parser just for a change.
            Assert.Equal(message, parser.Parse(json, TestWellKnownTypes.Descriptor));
        }

        [Fact]
        public void DataAfterObject()
        {
            string json = "{} 10";
            Assert.Throws<InvalidJsonException>(() => TestAllTypes.Parser.ParseText(json));
        }


        [Theory]
        [InlineData("AQI")]
        [InlineData("_-==")]
        public void Bytes_InvalidBase64(string badBase64)
        {
            string json = "singleBytes: \"" + badBase64 + "\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("\"FOREIGN_BAR\"", ForeignEnum.ForeignBar)]
        [InlineData("5", ForeignEnum.ForeignBar)]
        [InlineData("100", (ForeignEnum)100)]
        public void EnumValid(string value, ForeignEnum expectedValue)
        {
            string json = "singleForeignEnum: " + value;
            var parsed = TestAllTypes.Parser.ParseText(json);
            Assert.Equal(new TestAllTypes { SingleForeignEnum = expectedValue }, parsed);
        }

        [Theory]
        [InlineData("\"NOT_A_VALID_VALUE\"")]
        [InlineData("5.5")]
        public void Enum_Invalid(string value)
        {
            string json = "singleForeignEnum: " + value;
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Fact]
        public void OneofDuplicate_Invalid()
        {
            string json = "oneofString: \"x\"\noneofUint32: 10";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Fact]
        public void UnknownField_NotIgnored()
        {
            string json = "unknownField: 10\nsingleString: \"x\"";
            Assert.Throws<InvalidTextProtocolBufferException>(() => TestAllTypes.Parser.ParseText(json));
        }

        [Theory]
        [InlineData("5")]
        [InlineData("\"text\"")]
        [InlineData("[0, 1, 2]")]
        [InlineData("a: { b: 10 }")]
        public void UnknownField_Ignored(string value)
        {
            var parser = new TextParser(TextParser.Settings.Default.WithIgnoreUnknownFields(true));
            string json = "unknownField: " + value + "\nsingleString: \"x\"";
            var actual = parser.Parse<TestAllTypes>(json);
            var expected = new TestAllTypes { SingleString = "x" };
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Various tests use strings which have quotes round them for parsing or as the result
        /// of formatting, but without those quotes being specified in the tests (for the sake of readability).
        /// This method simply returns the input, wrapped in double quotes.
        /// </summary>
        internal static string WrapInQuotes(string text)
        {
            return '"' + text + '"';
        }
    }
}