namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Cricinfo.Models

type public DataCreationResponse =
    | Success = 0
    | Failure = 1
    | DuplicateContent = 2

type public ICricInfoRepository =
    abstract member CreateMatchAsync : Match -> Task<DataCreationResponse * Nullable<int64>>
    abstract member CreateTeamAsync : string -> Task<DataCreationResponse>
    abstract member DeleteMatchAsync : int -> Task<Unit>
    abstract member DeleteMatchAsync : string * string * DateTime -> Task<Unit>
    abstract member GetMatchAsync : int -> Task<Match>
    abstract member GetTeamsAsync : Unit -> Task<string[]>
    abstract member MatchExistsAsync : string * string * DateTime -> Task<bool>