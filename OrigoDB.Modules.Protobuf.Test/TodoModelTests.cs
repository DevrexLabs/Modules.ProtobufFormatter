using System;
using System.IO;
using System.Linq;
using Modules.ProtoBuf.Test.Domain;
using NUnit.Framework;
using OrigoDB.Core;
using OrigoDB.Core.Test;
using OrigoDB.Modules.ProtoBuf;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Modules.ProtoBuf.Test
{
    [TestFixture]
    public class TodoModelTests
    {
        [Test]
        public void TodoModel_can_be_serialized()
        {
            Assert.IsTrue(RuntimeTypeModel.Default.CanSerialize(typeof(TodoModel)));
        }

        [Test]
        public void Simple_inheritance_test()
        {
            TodoItem item = new SpecialTodoItem() {Title = "Go fishing"};
            var clone = Serializer.DeepClone(item);
            Assert.IsInstanceOf<SpecialTodoItem>(clone);
            Assert.AreEqual(item.Title, clone.Title);
            Assert.AreEqual(item.Id, clone.Id);
        }

        [Test]
        public void Polymorphism_test()
        {
            var model = BuildComplexModel();
            Dump(model);
            var clone = Serializer.DeepClone(model);
            Dump(clone);

            Assert.IsInstanceOf<SpecialTodoItem>(clone
                .Items
                .Values
                .Single(item => item.Title == "Bake a cake"));

            Assert.IsInstanceOf<SpecialTodoItem>(clone
                .Categories
                .Values
                .SelectMany(cat => cat.Items)
                .Single(item => item.Title == "Bake a cake")
                );
        }


        [Test]
        public void References_are_preserved()
        {
            var model = BuildComplexModel();
            Dump(model);
            var clone = Serializer.DeepClone(model);
            Dump(clone);
            
            //items in categories are same as items in items dictionary
            Assert.IsTrue(clone.Categories
                .SelectMany(c => c.Value.Items)
                .Distinct()
                .All(item => clone.Items.Values.Contains(item)));

            //duplicate items (by id) in multiple categories are same object ref
            Assert.IsTrue(clone.Categories
                .SelectMany(c => c.Value.Items)
                .GroupBy(item => item.Id)
                .All(g => g.Distinct().Count() == 1));
        }

        private void Dump(TodoModel model)
        {
            Console.WriteLine("---------------------------------------------------------------");
            foreach (var cat in model.Categories.Values)
            {
                Console.WriteLine("Category: <" + cat.Name + ">");
                foreach (var todoItem in cat.Items)
                {
                    Console.WriteLine("   --------Item-----------------------------------");
                    Console.WriteLine("   id:" + todoItem.Id);
                    Console.WriteLine("   title:" + todoItem.Title);
                    Console.WriteLine("   due:" + todoItem.Due);
                    Console.WriteLine("   completed:" + todoItem.Completed);
                    Console.WriteLine("   same ref: " + (model.Items[todoItem.Id] == todoItem));
                }
            }
        }

        [Test]
        public void Can_serialize_using_typed_formatter()
        {
            var formatter = Serializer.CreateFormatter<TodoModel>();
            formatter.Serialize(new MemoryStream(), BuildComplexModel());
        }

        private TodoModel BuildComplexModel()
        {
            var model = new TodoModel();
            var eat = model.AddItem("Eat");
            var sleep = model.AddItem("Sleep");
            var code = model.AddItem("Code");
            var play = model.AddItem("Play squash");
            var taxes = model.AddItem("Do taxes");
            var special = model.AddSpecialItem("Bake a cake");
            model.SetCategories(special, "Fun");
            model.SetCategories(taxes, "Work", "Boring");
            model.SetCategories(eat, "Fun", "Health");
            model.SetCategories(code, "Fun", "Work");
            model.SetCategories(sleep, "Boring", "Health");
            model.SetCategories(play, "Fun", "Health");
            return model;
        }

        [Test]
        public void Full_stack_smoke_test()
        {
            var config = new EngineConfiguration().ForIsolatedTest();
            ProtoBufFormatter.ConfigureSnapshots<TodoModel>(config, null);
            Engine<TodoModel> engine = Engine.Create(BuildComplexModel(), config);
            engine.CreateSnapshot();
            engine.Close();
            engine = Engine.Load<TodoModel>(config);
            var model = (TodoModel) engine.GetModel();
            Dump(model);
        }
    }
}