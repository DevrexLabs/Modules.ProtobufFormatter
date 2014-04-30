using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OrigoDB.Modules.Protobuf;
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
            var stream = SerializationHelper.Serialize<Employee>(graph, formatter);
            var result = SerializationHelper.Deserialize<Employee>(stream, formatter);

            // Act
            Assert.IsInstanceOf<Employee>(result);
            Assert.AreEqual("Kalle", result.Name);
            Assert.AreEqual(42, result.Age);
            Assert.AreEqual(16,result.ShoeSize);
        }

        [Test]
        public void CanDeserializeComplexType()
        {
            // Arrange
            var modelBuilder = new RuntimeTypeModelBuilder();
            modelBuilder.Add(typeof(Employee));
            var formatter = new ProtoBufFormatter();
            var graph = new Company() {
                Name = "Initech Corporation",
                Employees = new List<Employee> {
                        new Employee() { Name = "Peter Gibbons", Age = 34 },
                        new Employee() { Name = "Michael Bolton", Age = 39 }
                    }
            };

            //hack. triggers E#mployee type to be added to the TypeModel
            //SerializationHelper.Serialize(new Employee(), formatter);
            
            // Act
            var stream = SerializationHelper.Serialize<Company>(graph, formatter);
            var result = SerializationHelper.Deserialize<Company>(stream, formatter);

            // Act
            Assert.AreEqual("Initech Corporation", result.Name);
            Assert.IsNotNull(result.Employees);
            Assert.AreEqual(2, result.Employees.Count);
            Assert.AreEqual("Peter Gibbons", result.Employees.ElementAt(0).Name);
            Assert.AreEqual("Michael Bolton", result.Employees.ElementAt(1).Name);
        }

        [Test]
        public void NonSerializedTypeIsNotConsideredKnown()
        {
            var formatter = new ProtoBufFormatter();
            Assert.IsFalse(formatter.IsKnownType(typeof(Employee)));
        }

        [Test]
        public void SerializedTypeIsConsideredKnown()
        {
            var formatter = new ProtoBufFormatter();
            var graph = new Employee();
            var stream = SerializationHelper.Serialize<Employee>(graph, formatter);
            Assert.IsTrue(formatter.IsKnownType(typeof(Employee)));
        }
    }
}
