// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using EventStore.Client;

namespace Proto.Persistence.EventStore.Sample;

public class Worker : IHostedService {
    readonly EventStoreClient _client;
    readonly ActorSystem      _system;

    public Worker(EventStoreClient client) {
        _client = client;
        _system = new ActorSystem();
    }

    public Task StartAsync(CancellationToken cancellationToken) {
        var provider = new EventStoreProvider(_client);
        var props    = Props.FromProducer(() => new MyPersistenceActor(provider));

        var pid = _system.Root.Spawn(props);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => _system.ShutdownAsync();
}
