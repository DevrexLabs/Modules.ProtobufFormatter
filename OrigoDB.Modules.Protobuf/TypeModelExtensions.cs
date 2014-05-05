using System;
using System.Collections.Generic;
using System.Linq;
using OrigoDB.Core;
using OrigoDB.Core.Journaling;
using ProtoBuf.Meta;

namespace OrigoDB.Modules.ProtoBuf
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class TypeModelExtensions
    {
        /// <summary>
        /// Inejct contracts for origodb core types into given RuntimeTypeModel. Can safely be called multiple times.
        /// </summary>
        /// <param name="typeModel"></param>
        public static void RegisterFrameworkTypes(this RuntimeTypeModel typeModel)
        {
            if (typeModel.IsDefined(typeof (Command))) return;

            typeModel.Add(typeof(Command), true);

            var journalEntry = typeModel.Add(typeof (JournalEntry), false)
                .Add(1, "Id")
                .Add(2, "Created");

            journalEntry.UseConstructor = false;

            journalEntry.AddSubType(3, typeof(JournalEntry<Command>));
            journalEntry.AddSubType(4, typeof (JournalEntry<RollbackMarker>));
            journalEntry.AddSubType(5, typeof(JournalEntry<ModelCreated>));


            typeModel.Add(typeof(JournalEntry<Command>), false)
                .Add(1, "Item")
                .UseConstructor = false;

            typeModel.Add(typeof (JournalEntry<ModelCreated>), false)
                .Add(1, "Item")
                .UseConstructor = false;

            typeModel.Add(typeof (JournalEntry<RollbackMarker>), false)
                .Add(1, "Item")
                .UseConstructor = false;
            
            typeModel.Add(typeof (ModelCreated), false)
                .Add(1, "Type")
                .UseConstructor = false;

            typeModel.Add(typeof(RollbackMarker), false)
                .UseConstructor = false;

        }

        /// <summary>
        /// Register the passed commands as sub classes of OrigoDB.Core.Command. The id must be unique for each command
        /// and constant across versions of your system. If any of the command types are already registered nothing will happen.
        /// </summary>
        public static void RegisterCommandSubTypes(this RuntimeTypeModel typeModel, IDictionary<Type, int> commandIdsByType)
        {
            RegisterFrameworkTypes(typeModel);
            var commandMeta = typeModel[typeof(Command)];
            if (commandMeta.GetSubtypes().Any(st => commandIdsByType.ContainsKey(st.DerivedType.Type))) return;
            foreach (var type in commandIdsByType.Keys)
            {
                commandMeta.AddSubType(commandIdsByType[type], type);
            }
        }
    }
}