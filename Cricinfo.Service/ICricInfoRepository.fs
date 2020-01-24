﻿namespace Cricinfo.Services

open System
open System.Threading.Tasks
open Cricinfo.Models

type public DataCreationResponse =
    | Success = 0
    | Failure = 1
    | DuplicateContent = 2

type public ICricInfoRepository =
    abstract member GetMatchAsync : int -> Task<Match>
    abstract member CreateMatchAsync : Match -> Task<DataCreationResponse * Nullable<int64>>
    abstract member DeleteMatchAsync : int -> Task<Unit>
    abstract member DeleteMatchAsync : string * string * DateTime -> Task<Unit>