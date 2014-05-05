using System;
using System.Collections.Generic;
using System.Linq;
using Modules.ProtoBuf.Test.Domain;
using NUnit.Framework;
using OrigoDB.Core;
using OrigoDB.Core.Journaling;
using OrigoDB.Core.Test;
using OrigoDB.Modules.ProtoBuf;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Modules.ProtoBuf.Test
{
    [ProtoContract(SkipConstructor = true)]
    public class AddItemCommand : Command<TodoModel, Guid>
    {
        [ProtoMember(1)]
        public readonly string ItemName;

        public AddItemCommand(string itemName)
        {
            ItemName = itemName;
        }

        public override Guid Execute(TodoModel model)
        {
            return model.AddItem(ItemName);
        }
    }

    public class RemoveItemCommand : Command<TodoModel>
    {
        public readonly Guid Id;

        public RemoveItemCommand(Guid id)
        {
            Id = id;
        }

        public override void Execute(TodoModel model)
        {
            model.Items.Remove(Id);
        }
    }

    [TestFixture]
    public class JournalFormattingTests
    {
        EngineConfiguration _config;
        IStore _store;

        [SetUp]
        public void Setup()
        {
            _config = new EngineConfiguration().ForIsolatedTest();
            _config.PacketOptions = PacketOptions.Checksum;

            //assign unique ints to commands
            var commandTypeTags = new Dictionary<Type, int>
            {
                {typeof(AddItemCommand), 1},
                {typeof(RemoveItemCommand), 2}
            };

            //dynamic registration of command type (no attributes)
            var typeModel = TypeModel.Create();
            MetaType mt = typeModel.Add(typeof(RemoveItemCommand), false)
                .Add(1, "Id");
            mt.UseConstructor = false;


            ProtoBufFormatter.ConfigureJournaling(_config, commandTypeTags, typeModel);

            _store = _config.CreateStore();            
        }

        [Test]
        public void CanWriteaRollbackMarkerAndReadBack()
        {
            var writer = _store.CreateJournalWriter(0);
            writer.Write(new JournalEntry<RollbackMarker>(32, new RollbackMarker()));
            var resurrected = _store.GetJournalEntries().Single();
            Assert.AreEqual(32, resurrected.Id);
        }

        [Test]
        public void CanWriteModelCreatedEntryAndReadBack()
        {
            var writer = _store.CreateJournalWriter(0);
            writer.Write(new JournalEntry<ModelCreated>(32, new ModelCreated(typeof(TodoModel))));
            var resurrected = _store.GetJournalEntries().Single();
            Assert.AreEqual(32, resurrected.Id);
            Assert.AreEqual(typeof(TodoModel),(resurrected as JournalEntry<ModelCreated>).Item.Type);
        }

        [Test]
        public void CanWriteCommandToJournalAndReadBack()
        {
            const string itemName = "Fish";
            var entryTimestamp = DateTime.Now;

            var writer = _store.CreateJournalWriter(0);
            var entry = new JournalEntry<Command>(1, new AddItemCommand(itemName), entryTimestamp);
            writer.Write(entry);
            var resurrected = _store.GetJournalEntries().Single();

            Assert.AreEqual(entryTimestamp, resurrected.Created);
            Assert.AreEqual(1, resurrected.Id);
            Assert.IsInstanceOf<JournalEntry<Command>>(resurrected);
            var typedEntry = (JournalEntry<Command>)resurrected;
            Assert.IsInstanceOf<AddItemCommand>(typedEntry.Item);
            var typedCommand = (AddItemCommand)typedEntry.Item;
            Assert.AreEqual(itemName, typedCommand.ItemName);
        }
    }
}