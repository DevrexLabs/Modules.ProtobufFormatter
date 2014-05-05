using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using OrigoDB.Core;
using ProtoBuf;
using ProtoBuf.Meta;
using OrigoDB.Core.Utilities;

namespace OrigoDB.Modules.ProtoBuf
{
    /// <summary>
    /// Provides functionality for formatting serialized objects
    /// using ProtoBuf serialization.
    /// </summary>
    public class ProtoBufFormatter : IFormatter
    {
        readonly RuntimeTypeModel _typeModel;


        /// <summary>
        /// Prefix each serialized object with its assembly qualified name
        /// </summary>
        public readonly bool IncludeTypeName;

        /// <summary>
        /// Write the length of the serialized object as part of the header.
        /// Necessary when sending multiple objects to the same stream,
        /// avoid for large graphs because they must be buffered in memory
        /// in order to figure out the size.
        /// </summary>
        public readonly bool UseLengthPrefix;


        /// <summary>
        /// The associated serialization binder.
        /// </summary>
        public SerializationBinder Binder
        {
            get;
            set;
        }

        /// <summary>
        /// The associated streaming context.
        /// </summary>
        public StreamingContext Context
        {
            get;
            set;
        }

        /// <summary>
        /// The associated surrogate selector.
        /// </summary>
        public ISurrogateSelector SurrogateSelector
        {
            get { return null; }
            set { /* Do nothing here since it's not used. */; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrigoDB.Modules.ProtoBuf.ProtoBufFormatter"/> class.
        /// </summary>
        public ProtoBufFormatter(RuntimeTypeModel typeModel = null, bool includeTypeName = true, bool useLengthPrefix = false)
        {
            IncludeTypeName = includeTypeName;
            UseLengthPrefix = useLengthPrefix;
            _typeModel = typeModel ?? TypeModel.Create();
            Context = new StreamingContext(StreamingContextStates.Persistence);
        }


        /// <summary>
        /// Deserializes the data on the provided stream and 
        /// reconstitutes the graph of objects.
        /// </summary>
        /// <param name="stream">The stream that contains the data to deserialize.</param>
        /// <returns>The top object of the deserialized graph.</returns>
        public object Deserialize(Stream stream)
        {
            Ensure.NotNull(stream, "stream");

            var type = GetType(stream);

            return UseLengthPrefix
                ? _typeModel.DeserializeWithLengthPrefix(stream, null, type, PrefixStyle.Base128, 0)
                : _typeModel.Deserialize(stream, null, type);
        }

        /// <summary>
        /// Serializes an object, or graph of objects with the given root to the provided stream.
        /// </summary>
        /// <param name="stream">The stream where the formatter puts the serialized data. This stream can
        /// reference a variety of backing stores (such as files, network, memory, and so on).</param>
        /// <param name="graph">The object, or root of the object graph, to serialize.
        /// All child objects of this root object are automatically serialized.</param>
        public void Serialize(Stream stream, object graph)
        {
            Ensure.NotNull(stream, "stream");
            Ensure.NotNull(graph, "graph");

            if (IncludeTypeName)
                new BinaryWriter(stream, Encoding.UTF8)
                .Write(graph.GetType().AssemblyQualifiedName);

            if (UseLengthPrefix)
                _typeModel.SerializeWithLengthPrefix(stream, graph, graph.GetType(), PrefixStyle.Base128, 0);
            else
                _typeModel.Serialize(stream, graph);
        }

        /// <summary>
        /// Determine what type to read when deserializing by reading the type name from the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected virtual Type GetType(Stream stream)
        {
            Type result = null;
            if (IncludeTypeName)
            {
                var typeName = new BinaryReader(stream, Encoding.UTF8).ReadString();
                result = Type.GetType(typeName);
            }
            return result;
        }

        /// <summary>
        /// Modify the given configuration to use ProtoBuf formatting to clone results
        /// </summary>
        /// <param name="config"></param>
        /// <param name="typeModel">An optional typemodel</param>
        public static void ConfigureResultCloning(EngineConfiguration config, RuntimeTypeModel typeModel = null)
        {
            config.SetFormatterFactory((cfg, fu) => new ProtoBufFormatter(typeModel, true, true), FormatterUsage.Results);
        }

        /// <summary>
        /// Modify the given configuration to use ProtoBuf formatting for snapshots of type T.
        /// </summary>
        /// <typeparam name="T">The concrete type of the model</typeparam>
        public static void ConfigureSnapshots<T>(EngineConfiguration config, RuntimeTypeModel typeModel = null) where T : Model
        {
            config.SetFormatterFactory((cfg, fu) => new ProtoBufFormatter<T>(typeModel), FormatterUsage.Snapshot);
        }

        /// <summary>
        /// Modify the given EngineConfiguration to use ProtoBuf for journaling. Pass unique ints for each type of command.
        /// The id's must be maintained across versions of your assembly.
        /// </summary>
        public static void ConfigureJournaling(EngineConfiguration config, IDictionary<Type, int> commandTypeTags, RuntimeTypeModel typeModel = null)
        {
            config.SetFormatterFactory((cfg, fu) =>
            {
                var formatter = new ProtoBufFormatter<JournalEntry>(typeModel, includeTypeName: false, useLengthPrefix: true);
                typeModel.RegisterCommandSubTypes(commandTypeTags);
                return formatter;
            }, 
            FormatterUsage.Journal);
        }
    }

    /// <summary>
    /// ProtoBuf IFormatter implementation which serializes/deserializes object of type T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ProtoBufFormatter<T> : ProtoBufFormatter
    {
        /// <summary>
        /// Create a typed formatter. Type name won't be prepended to the stream because the type is known.
        /// </summary>
        public ProtoBufFormatter(RuntimeTypeModel typeModel = null, bool useLengthPrefix = false, bool includeTypeName = false)
            : base(includeTypeName: includeTypeName, typeModel: typeModel, useLengthPrefix: useLengthPrefix)
        { }

        /// <summary>
        /// Derive the type from the generic type parameter as opposed to reading the type name from the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected override Type GetType(Stream stream)
        {
            return typeof(T);
        }
    }
}
