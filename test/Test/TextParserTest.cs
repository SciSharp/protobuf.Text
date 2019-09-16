using System;
using NUnit.Framework;
using Google.Protobuf.Reflection;

namespace Tests
{
    public class TextParserTest
    {
        [SetUp]
        public void Setup(MessageDescriptor descriptor)
        {
            Console.WriteLine(descriptor.File);
        }

        [Test]
        public void TestName()
        {

        }
    }
}