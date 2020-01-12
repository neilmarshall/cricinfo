namespace DbCreate

module Logging =
    type Logger = | Debug of string | Info of string | Error of string

    let logMessage =
        function
        | Debug msg -> printfn "DEBUG: %s" msg
        | Info msg -> printfn "INFO: %s" msg
        | Error msg -> printfn "ERROR: %s" msg
