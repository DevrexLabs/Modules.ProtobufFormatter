using System;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using OrigoDB.Core;
using OrigoDB.Core.Journaling;
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
            get; set;
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
        /// Dynamically configure the origodb types written to the journal
        /// </summary>
        private void RegisterJournalTypes()
        {
            //TODO: incomplete
            var je = _typeModel.Add(typeof(JournalEntry), false);
            je.AddField(1, "Id");
            je.AddField(2, "Created");
            je.AddSubType(3, typeof(JournalEntry<RollbackMarker>));
            je.AddSubType(4, typeof(JournalEntry<Command>)).Add(1, "Item");
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
        /// Modify the given configuration to use ProtoBuf formatting
        /// </summary>
        /// <param name="config"></param>
        /// <param name="usage">the usage to configure</param>
        /// <param name="typeModel">An optional typemodel</param>
        public static void Configure(EngineConfiguration config, FormatterUsage usage, RuntimeTypeModel typeModel = null)
        {
            switch (usage)
            {
                case FormatterUsage.Default:
                    throw new NotSupportedException("Call with a specific usage, not FormatterUsage.Default");
                case FormatterUsage.Snapshot:
                    throw new NotSupportedException("Can't configure for snapshots without a type parameter, call ConfigureSnapshots<T> passing the type of the model instead");
                case FormatterUsage.Journal:
                    config.SetFormatterFactory((cfg, fu) => new ProtoBufFormatter<JournalEntry>(typeModel: typeModel, useLengthPrefix: true), usage);
                    break;
                case FormatterUsage.Results:
                    config.SetFormatterFactory((cfg,fu) => new ProtoBufFormatter(typeModel, true, true), usage);
                    break;
                case FormatterUsage.Messages:
                    throw new NotSupportedException("Not supported in this version of the module");
                default:
                    throw new ArgumentOutOfRangeException("usage");
            }
        }

        /// <summary>
        /// Modify the given configuration to use ProtoBuf formatting for snapshots.
        /// </summary>
        /// <typeparam name="T">The concrete type of the model</typeparam>
        /// <param name="config">the configuration to modify</param>
        /// <param name="typeModel">An optional runtime type configuration</param>
        public static void ConfigureSnapshots<T>(EngineConfiguration config, RuntimeTypeModel typeModel)
        {
            config.SetFormatterFactory((cfg,fu) => new ProtoBufFormatter<T>(typeModel: typeModel), FormatterUsage.Snapshot);
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
        /// <param name="typeModel"></param>
        /// <param name="useLengthPrefix"></param>
        public ProtoBufFormatter(RuntimeTypeModel typeModel = null, bool useLengthPrefix = false) 
            : base(includeTypeName: false, typeModel: typeModel, useLengthPrefix: useLengthPrefix)
        {}

        /// <summary>
        /// Derive the type from the generic type parameter as opposed to reading the type name from the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected override Type GetType(Stream stream)
        {
            return typeof (T);
        }
    }
}
