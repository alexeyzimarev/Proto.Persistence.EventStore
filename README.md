# EventStore persistence for Proto.Actor

Tha library implements persisting events and snapshots for the [Proto.Actor](http://proto.actor) actor model framework.

## Usage

Install the package to your solution:

```
dotnet add nuget Proto.Persistence.EventStore
```

Create an EventStore connection in your startup code and ask it to connect.

```csharp
var esConnection = EventStoreConnection.Create(
    "ConnectTo=tcp://admin:changeit@localhost:1113; DefaultUserCredentials=admin:changeit;",
    ConnectionSettings.Create().KeepReconnecting(),
    "Proto.Persistence.EventStore.Sample");

await esConnection.ConnectAsync();
```

Then, configure the persistence for your actor:

```csharp
var provider = new EventStoreProvider(esConnection);
var props = Props.FromProducer(() => new MyPersistenceActor(provider));
```

From here, everything should work, including snapshots.

Don't forget to close the connection when your app stops:

```csharp
esConnection.Close();
```

## Sample

Check `Proto.Persistence.EventStore.Sample` to see stuff working.

Use `docker-compose up` to start the EventStore in a container. The `docker-compose.yml` file is located in the sample project directory. Check the web UI at [http://localhost:2113](http://localhost:2113) with user `user` and password `changeit`, look at the Stream Browser to see the actor stream and the snapshot stream.

When the app runs, it only adds events and snapshots. If you stop the app and run it again, it will recover actors by reading snapshots and events and then continue adding new events.
