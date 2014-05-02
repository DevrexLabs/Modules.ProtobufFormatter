This is a custom IFormatter implementation based on the
protobuf-net library by Marc Gravell, available at http://code.google.com/p/protobuf-net/


To serialize using protobuf you need to annotate your types with ProtoContract and ProtoMember or
configure a RunTimeTypeModel object and pass it to the ProtoBufFormatter constructor.

To learn read, the protobuf-net docs, see examples in the protobuf-net source and the source
of this repository.


Once you have configured your types, there are a couple of static methods to help
you get the OrigoDB configuration right:

```csharp

  //Annotate your model and entities with ProtoContract and ProtoMember
  // or create a type model from code:
  var typeModel = TypeModel.Create();
  //todo: add types, fields and subtypes to the typeModel
  
  
  var config = new EngineConfiguration();
  
  //use helper methods
  ProtoBufFormatter.ConfigureSnapshots<MyModel>(config, typeModel);
  ProtoBufFormatter.Configure(config, FormatterUsage.Results, typeModel);
  
  //or do it yourself
  config.SetFormatterFactory((cfg,fu) => new ProtoBufFormatter<MyModel>(), FormatterUsage.Snapshot);
  
  //use the config when creating an engine
  var db = Db.For<MyModel>(config);
  db.WriteSnapshot();
```


