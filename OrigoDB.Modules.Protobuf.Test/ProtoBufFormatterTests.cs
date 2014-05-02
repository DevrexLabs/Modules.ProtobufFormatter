using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OrigoDB.Modules.ProtoBuf;
using System.Runtime.Serialization;
using Modules.ProtoBuf.Test.Framework;
using Modules.ProtoBuf.Test.Domain;

namespace Modules.ProtoBuf.Test
{
    [TestFixture]
    public class ProtoBufFormatterTests
    {
        [Test]
        public void FormatterHasStreamingContext()
        {
            var formatter = new ProtoBufFormatter();
            Assert.IsNotNull(formatter.Context);
        }

        [Test]
        public void SerializationContextStateIsSetToPersistence()
        {
            var formatter = new ProtoBufFormatter();
            Assert.AreEqual(StreamingContextStates.Persistence, formatter.Context.State);
        }


        [Test]
        public void CanDeserializeType()
        {
            // Arrange
            var formatter = new ProtoBufFormatter();
            var graph = new Employee(16) { Name = "Kalle", Age = 42 };

            // Act
            var result = SerializationHelper.Clone<Employee>(graph, formatter);

            // Assert
            Assert.IsInstanceOf<Employee>(result);
            Assert.AreEqual("Kalle", result.Name);
            Assert.AreEqual(42, result.Age);
            Assert.AreEqual(16,result.ShoeSize);
        }

        [Test]
        public void CanDeserializeComplexType()
        {
            // Arrange
            var formatter = new ProtoBufFormatter<Company>();
            var graph = new Company() {
                Name = "Initech Corporation",
                Employees = new List<Employee> {
                        new Employee() { Name = "Peter Gibbons", Age = 34 },
                        new Employee() { Name = "Michael Bolton", Age = 39 }
                    }
            };

            // Act
            var result = SerializationHelper.Clone<Company>(graph, formatter);

            // Assert
            Assert.AreEqual("Initech Corporation", result.Name);
            Assert.IsNotNull(result.Employees);
            Assert.AreEqual(2, result.Employees.Count);
            Assert.AreEqual("Peter Gibbons", result.Employees.ElementAt(0).Name);
            Assert.AreEqual("Michael Bolton", result.Employees.ElementAt(1).Name);
        }
    }
}
