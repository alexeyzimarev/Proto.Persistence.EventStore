// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Text;
using Messages;

namespace Proto.Persistence.EventStore.Sample; 

class LoopActor : IActor {
    class LoopParentMessage { }

    public Task ReceiveAsync(IContext context) {
        switch (context.Message) {
            case Started:
                Console.WriteLine("LoopActor - Started");
                context.Send(context.Self, new LoopParentMessage());
                break;
            case LoopParentMessage:
                Task.Run(
                    async () => {
                        context.Send(context.Parent, new RenameCommand { Name = GeneratePronounceableName(5) });
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                        context.Send(context.Self, new LoopParentMessage());
                    }
                );

                break;
        }

        return Task.CompletedTask;
    }

    static string GeneratePronounceableName(int length) {
        const string vowels     = "aeiou";
        const string consonants = "bcdfghjklmnpqrstvwxyz";

        var rnd  = new Random();
        var name = new StringBuilder();

        length = length % 2 == 0 ? length : length + 1;

        for (var i = 0; i < length / 2; i++) {
            name
                .Append(vowels[rnd.Next(vowels.Length)])
                .Append(consonants[rnd.Next(consonants.Length)]);
        }

        return name.ToString();
    }
}
