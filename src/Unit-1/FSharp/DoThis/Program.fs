open System
open Akka.FSharp
open WinTail.Actors
open WinTail.Messages

[<EntryPoint>]
let main _ = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load())
        
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf consoleWriterActor)
    // New actor to validate messages
    let validationActor = spawn myActorSystem "validationActor"
                              (actorOf2 (validationActor consoleWriterActor))
    let consoleReaderActor =
        spawn myActorSystem "consoleReaderActor" (actorOf2 (consoleReaderActor validationActor))

    consoleReaderActor <! Start

    myActorSystem.WhenTerminated.Wait ()
    0
