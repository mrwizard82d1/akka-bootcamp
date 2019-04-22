namespace WinTail

open System
open Akka.Actor
open Akka.FSharp
open Messages

module Actors =
    
    // Active pattern matching to determine overall nature of input 
    let (|Message|Exit|) (str:string) =
        match str.ToLower() with
        | "exit" -> Exit
        | _ -> Message(str)

    // Active pattern matching to determine the characteristics of the message (empty, even or odd length)
    let (|EmptyMessage|MessageLengthIsEven|MessageLengthIsOdd|) (msg:string) =
        match msg.Length, msg.Length % 2 with
        | 0, _ -> EmptyMessage
        | _, 0 -> MessageLengthIsEven
        | _, _ -> MessageLengthIsOdd
        
    // Print instructions to the console
    let doPrintInstructions () =
        printfn "Write whatever you want into the console!"
        printfn "Some entries will pass validation, and some won't...\n\n"
        printfn "Type 'exit' to quit this application at anytime.\n"
        
    let consoleReaderActor (consoleWriter: IActorRef) (mailbox: Actor<_>) message =
        
        let getAndValidateInput () =
            let line = Console.ReadLine ()
            match line with
            | Exit -> mailbox.Context.System.Terminate() |> ignore
            | Message(input) ->
                match input with
                | EmptyMessage ->
                    mailbox.Self <! InputError ("No input received.", ErrorType.Null)
                | MessageLengthIsEven ->
                    consoleWriter <! InputSuccess ("Thank you. The message was valid.")
                    mailbox.Self <! Continue
                | MessageLengthIsOdd ->
                    mailbox.Self <! InputError ("The message is invalid (odd number of characters).",
                                                ErrorType.Validation)
            
        // Begin by handling the incoming `message`
        match box message with // `box message` wraps `message` as an `Object` for use by the `:?` operator
        | :? Command as command ->
            match command with
            | Start -> doPrintInstructions () // Print instructions at the start
            | _ -> () // Otherwise, do nothing
        | :? InputResult as inputResult ->
            match inputResult  with
            | InputError (_, _) as error -> consoleWriter <! error
            | _ -> ()
        | _ -> ()
        
        // Once we've handled the message, wait (block) awaiting user input
        getAndValidateInput()

    let consoleWriterActor message = 
        let printInColor color message =
            Console.ForegroundColor <- color
            Console.WriteLine (message.ToString ())
            Console.ResetColor ()

        match box message with
        | :? InputResult as inputResult ->
            match inputResult with
            | InputError (reason, _) -> printInColor ConsoleColor.Red reason
            | InputSuccess reason -> printInColor ConsoleColor.Green reason
        | _ -> printInColor ConsoleColor.Yellow (message.ToString ())
                
