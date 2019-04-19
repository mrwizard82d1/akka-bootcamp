// Learn more about F# at http://fsharp.org

open System.Threading
open Akkling
open WinTail

[<EntryPoint>]
let main argv =
    use system = System.create "MyActorSystem" (Configuration.defaultConfig())
    let consoleWriterActor =
        spawn system "console-writer-actor" (props (Behaviors.printf "%s"))
    let consoleReaderActor =
        spawn system "console-reader-actor" <| props (actorOf (ConsoleActorReader.behavior consoleWriterActor))
        
    consoleReaderActor <! ConsoleActorReader.Input "fooey"
    consoleReaderActor <! ConsoleActorReader.Exit ""
    
    Thread.Sleep(100)
    
    0 // return an integer exit code
