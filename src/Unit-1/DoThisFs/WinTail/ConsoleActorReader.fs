namespace WinTail

open Akkling

module ConsoleActorReader =
    type Messages =
        | Exit of string
        | Input of string
        
    let rec behavior consoleWriterActor = function
        | Exit _ ->
            printfn "Terminate system" |> ignored
        | Input text ->
            consoleWriterActor <! text
            become (behavior consoleWriterActor)

