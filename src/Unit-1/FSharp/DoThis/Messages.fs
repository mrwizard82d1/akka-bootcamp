namespace WinTail

module Messages =
    open Akka.Actor
    
    // Commands
    type Command = 
    | Start
    | Continue
    | Message of string
    | Exit
    
    // Detailed error information
    type ErrorType =
        | Null // Empty or blank
        | Validation // Invalid
        
    // What is the result of validating the supplied input
    type InputResult =
        | InputSuccess of string
        | InputError of reason:string * errorType:ErrorType
        
    // Messages to start and stop observing file content for changes
    type TailCommand =
        | StartTail of filePath:string * reporterActor:IActorRef // file to observe, actor to display contentA
        | StopTail of filePath:string
        
    type FileCommand =
        | FileWrite of fileName:string
        | FileError of fileName:string * reason:string
        | InitialRead of fileName:string * text:string
  