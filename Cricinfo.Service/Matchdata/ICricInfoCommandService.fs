﻿namespace Cricinfo.Services.Matchdata

open System
open System.Threading.Tasks
open Cricinfo.Models

type public DataCreationResponse =
    | Success = 0
    | Failure = 1
    | DuplicateContent = 2

type public ICricInfoCommandService =
    abstract member CreateMatchAsync : Match -> Task<DataCreationResponse * Nullable<int64>>
    abstract member CreateTeamAsync : string -> Task<DataCreationResponse>
    abstract member DeleteMatchAsync : int -> Task<unit>
    abstract member DeleteMatchAsync : string * string * DateTime -> Task<unit>
