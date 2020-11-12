namespace Cricinfo.Services.Matchdata

open System
open System.Threading.Tasks
open Cricinfo.Models

type public ICricInfoQueryService =
    abstract member GetMatchAsync : int -> Task<Match>
    abstract member GetAllMatchesAsync : unit -> Task<Match[]>
    abstract member GetTeamsAsync : unit -> Task<string[]>
    abstract member MatchExistsAsync : string * string * DateTime -> Task<bool>
