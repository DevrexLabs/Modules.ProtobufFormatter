using NUnit.Framework;
using OrigoDB.Modules.ProtoBuf;
using Modules.ProtoBuf.Test.Framework;
using Modules.ProtoBuf.Test.Domain;

namespace Modules.ProtoBuf.Test
{
    [TestFixture]
    public class ProtoBufStreamHeaderTests
    {
        [Test]
        public void HeaderReadFromStreamContainsCorrectLength()
        {
            var stream = SerializationHelper.Serialize<Employee>(new Employee());
            var expectedHeader = ProtoBufStreamHeader.Create(typeof(Employee));
            var header = ProtoBufStreamHeader.Read(stream);

            // Assert
            Assert.AreEqual(expectedHeader.Length, header.Length);
        }

        [Test]
        public void HeaderReadFromStreamContainsCorrectTypeName()
        {
            var stream = SerializationHelper.Serialize<Employee>(new Employee());
            var expectedHeader = ProtoBufStreamHeader.Create(typeof(Employee));
            var header = ProtoBufStreamHeader.Read(stream);

            // Assert
            Assert.AreEqual(expectedHeader.TypeName, header.TypeName);
        }

        [Test]
        public void HeaderReadFromStreamContainsCorrectBuffer()
        {
            var stream = SerializationHelper.Serialize<Employee>(new Employee());
            var expectedHeader = ProtoBufStreamHeader.Create(typeof(Employee));
            var header = ProtoBufStreamHeader.Read(stream);

            // Assert
            CollectionAssert.AreEqual(expectedHeader.Buffer, header.Buffer);
        }
    }
}
