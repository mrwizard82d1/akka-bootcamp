open System
open Akka.Actor
open Akka.FSharp
open WinTail.Actors
open WinTail.Messages

[<EntryPoint>]
let main _ = 
    let myActorSystem = System.create "MyActorSystem" (Configuration.load())
        
    let consoleWriterActor = spawn myActorSystem "consoleWriterActor" (actorOf consoleWriterActor)
    
    // Supervision strategy used by tailCoordinatorActor
    let strategy () =
        Strategy.OneForOne((fun ex ->
            match ex with
            | :? ArithmeticException -> Directive.Resume
            | :? NotSupportedException -> Directive.Stop
            | _ -> Directive.Restart),
            10, // maximum of 10 restarts
            TimeSpan.FromSeconds(30.)) // within 30 seconds
    
    let tailCoordinatorActor =
        spawnOpt myActorSystem
            "tailCoordinatorActor"
            (actorOf2 tailCoordinatorActor)
            [ SpawnOption.SupervisorStrategy (strategy ()) ]
            
    // Pass tailCoordinatorActor to fileValidatorActor props (adding just one argument)
    let validationActor = spawn myActorSystem "validationActor"
                              (actorOf2 (fileValidationActor consoleWriterActor tailCoordinatorActor))
    let consoleReaderActor =
        spawn myActorSystem "consoleReaderActor" (actorOf2 (consoleReaderActor validationActor))

    consoleReaderActor <! Start

    myActorSystem.WhenTerminated.Wait ()
    0
