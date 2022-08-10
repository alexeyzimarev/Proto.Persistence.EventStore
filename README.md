# EventStoreDB persistence for Proto.Actor

Tha library implements persisting events and snapshots for the [Proto.Actor](http://proto.actor) actor model framework using [EventStoreDB](https://eventstore.com).

The latest version of the library supports only the gRPC protocol and can only be used with EventStoreDB version 20+.

## Usage

Install the package to your solution:

```
dotnet add nuget Proto.Persistence.EventStore
```

Create an EventStore connection in your startup code and ask it to connect.

```csharp
var eventStoreClient = EventStoreClient.Create("esdb://localhost?tls=false")
```

Then, configure the persistence for your actor:

```csharp
var provider = new EventStoreProvider(eventStoreClient);
var props = Props.FromProducer(() => new MyPersistenceActor(provider));
```

From here, everything should work, including snapshots.

## Sample

Check `Proto.Persistence.EventStore.Sample` to see stuff working.

Use `docker-compose up` to start the EventStoreDB in a container. 
The `docker-compose.yml` file is located in the sample project directory, and it uses the ARM64 Docker image of EventStoreDB. Change the image to `eventstore/eventstore` to run it on x64 machine.
Check the web UI at [http://localhost:2113](http://localhost:2113) with user `user` and password `changeit`, look at the Stream Browser to see the actor stream and the snapshot stream.

When the app runs, it only adds events and snapshots. If you stop the app and run it again, it will recover actors by reading snapshots and events and then continue adding new events.
