// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Messages;
using Proto.Persistence.SnapshotStrategies;

namespace Proto.Persistence.EventStore.Sample; 

class MyPersistenceActor : IActor {
    PID                  _loopActor;
    State                _state = new();
    readonly Persistence _persistence;

    public MyPersistenceActor(IProvider provider)
        => _persistence = Persistence.WithEventSourcingAndSnapshotting(
            provider,
            provider,
            "demo-app-id",
            ApplyEvent,
            ApplySnapshot,
            new IntervalStrategy(20),
            () => _state
        );

    void ApplyEvent(Event @event) {
        switch (@event) {
            case RecoverEvent msg:
                if (msg.Data is RenameEvent re) {
                    _state.Name = re.Name;

                    Console.WriteLine(
                        "MyPersistenceActor - RecoverEvent = Event.Index = {0}, Event.Data = {1}",
                        msg.Index,
                        msg.Data
                    );
                }

                break;
            case ReplayEvent msg:
                if (msg.Data is RenameEvent rp) {
                    _state.Name = rp.Name;

                    Console.WriteLine(
                        "MyPersistenceActor - ReplayEvent = Event.Index = {0}, Event.Data = {1}",
                        msg.Index,
                        msg.Data
                    );
                }

                break;
            case PersistedEvent msg:
                Console.WriteLine(
                    "MyPersistenceActor - PersistedEvent = Event.Index = {0}, Event.Data = {1}",
                    msg.Index,
                    msg.Data
                );

                break;
        }
    }

    void ApplySnapshot(Snapshot snapshot) {
        switch (snapshot) {
            case RecoverSnapshot msg:
                if (msg.State is State ss) {
                    _state = ss;

                    Console.WriteLine(
                        "MyPersistenceActor - RecoverSnapshot = Snapshot.Index = {0}, Snapshot.State = {1}",
                        _persistence.Index,
                        ss.Name
                    );
                }

                break;
        }
    }

    class StartLoopActor { }

    bool _timerStarted;

    public async Task ReceiveAsync(IContext context) {
        switch (context.Message) {
            case Started:
                Console.WriteLine("MyPersistenceActor - Started");
                Console.WriteLine("MyPersistenceActor - Current State: {0}", _state);
                await _persistence.RecoverStateAsync();
                context.Send(context.Self, new StartLoopActor());
                break;
            case StartLoopActor msg:
                await Handle(context, msg);
                break;
            case RenameCommand msg:
                await Handle(msg);
                break;
        }
    }

    Task Handle(ISpawnerContext context, StartLoopActor message) {
        if (_timerStarted) return Task.CompletedTask;

        _timerStarted = true;

        Console.WriteLine("MyPersistenceActor - StartLoopActor");

        var props = Props.FromProducer(() => new LoopActor());

        _loopActor = context.Spawn(props);

        return Task.CompletedTask;
    }

    async Task Handle(RenameCommand message) {
        Console.WriteLine("MyPersistenceActor - RenameCommand");

        _state.Name = message.Name;

        await _persistence.PersistEventAsync(new RenameEvent { Name = message.Name });
    }
}
