
This is a custom IFormatter implementation based on the
protobuf-net library by Marc Gravell, available at http://code.google.com/p/protobuf-net/

By default, the formatter writes type information to the stream which is needed during deserialization.

Usage:
var formatter = new ProtoBufFormatter();
formatter.Serialize(stream, graph);
object cloned = formatter.Deserialize();
