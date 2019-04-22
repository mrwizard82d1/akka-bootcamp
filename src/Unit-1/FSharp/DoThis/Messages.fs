namespace WinTail

module Messages =
    
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
