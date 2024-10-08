namespace Library

open System

[<RequireQualifiedAccess>]
module Option =
    let fromTryTuple = function
        | false, _ -> None
        | true, x -> Some x

[<RequireQualifiedAccess>]
module Result =
    let fromOption errorValue = function
        | Some x -> Ok x
        | None -> Error errorValue

module Parser =
    let tryParse (input:string) =
        Int32.TryParse input
        |> Option.fromTryTuple
        
module Validator =
    type ValidNumber = private ValidNumber of int
    module ValidNumber =
        let isValidNumber number =
            match 1 <= number && number <= 4000 with
            | false -> None
            | true -> Some <| ValidNumber number
        let value (ValidNumber number) = number
        
module FizzBuzz =
    open Validator 
    let getFizzBuzz validNumber =
        [1 .. ValidNumber.value validNumber]
        |> List.map (fun n -> (n, n % 3, n % 5))
        |> List.map (function
            | (_,0,0) -> "FizzBuzz"
            | (_,0,_) -> "Fizz"
            | (_,_,0) -> "Buzz"
            | (n,_,_) -> string n)
        |> String.concat "\n"
    
module Domain =
    open Validator
    
    type ParserNumber = string -> int option
    type ValidateNumber = int -> ValidNumber option
    type GetFizzBuzz = ValidNumber -> string
    
    type ParserError = NotANumber of string
    type ValidatorError = InvalidNumber of int
    
    type Error =
        | ParserError of ParserError
        | ValidatorError of ValidatorError
        
    type ExecuteFizzBuzzWorkflow = string -> Result<string, Error>
    
    let execute
        (parserNumber: ParserNumber)
        (validateNumber: ValidateNumber)
        (getFizzBuzz: GetFizzBuzz) : ExecuteFizzBuzzWorkflow =
        let parseNumber input =
            input
            |> parserNumber
            |> Result.fromOption (NotANumber input)
            |> Result.mapError ParserError
        
        let validateNumber number =
            number
            |> validateNumber
            |> Result.fromOption (InvalidNumber number)
            |> Result.mapError ValidatorError
            
        fun input ->
            input
            |> parseNumber
            |> Result.bind validateNumber
            |> Result.map getFizzBuzz
            
module Application =
    open Domain
    
    type Input = unit -> string
    type Output = string -> unit
    
    let execute =
        Domain.execute
            Parser.tryParse
            Validator.ValidNumber.isValidNumber
            FizzBuzz.getFizzBuzz
    
    let application (input:Input) (output:Output) =
        fun () ->
            output "Please enter a number between 1 and 4000:"
            input ()
            |> execute
            |> function
                | Ok s ->
                    sprintf $"Here is the output: \n%s{s}"
                    |> output
                | Error (ParserError (NotANumber s)) ->
                    sprintf $"%s{s} is not an integer"
                    |> output
                | Error (ValidatorError (InvalidNumber num)) ->
                    sprintf $"You entered %i{num}. Please enter a valid integer between 1 and 4000."
                    |> output