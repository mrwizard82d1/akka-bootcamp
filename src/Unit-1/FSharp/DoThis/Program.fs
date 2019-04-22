open System
open Akka.FSharp
open WinTail

[<EntryPoint>]
let main _ = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load())
        
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf Actors.consoleWriterActor)
    let consoleReaderActor =
        spawn myActorSystem "consoleReaderActor" (actorOf2 (Actors.consoleReaderActor consoleWriterActor))

    consoleReaderActor <! Messages.Start

    myActorSystem.WhenTerminated.Wait ()
    0
