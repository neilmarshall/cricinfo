module private Cricinfo.Services.IdentityStore.PostgresDbQueryFunctions

open System
open System.Linq
open Microsoft.AspNetCore.Identity
open Cricinfo.Services.IdentityStore.Models
open Cricinfo.Services.PostgresDbQueryFunctions

let genericQueryWrapper = genericQueryWrapper

let private claimResponseMapper = fun (response : Data.Common.DbDataReader) ->
    async {
        return IdentityUserClaim<int>(
            Id = response.GetInt32(0),
            UserId = response.GetInt32(1),
            ClaimType = response.GetString(2),
            ClaimValue = response.GetString(3))
    }

let private userResponseMapper connString = fun (response : Data.Common.DbDataReader) ->
    async {
        let id = response.GetInt32(0)
        let userName = response.GetString(1)
        let passwordHash = response.GetString(2)
        let! claimIds = queryRecordSet<int> connString "SELECT id FROM users.claim WHERE user_id = @user_id;" (Map.ofList ["user_id", id :> obj])
        let getClaim (claimId : int) =
            async {
                return! queryRecord connString "SELECT * FROM users.claim WHERE id = @claim_id;" (Map.ofList ["claim_id", claimId :> obj]) claimResponseMapper
            }
        let! claimTasks = claimIds |> Seq.map getClaim |> Async.Sequential
        let claims = claimTasks |> Array.filter (fun o -> Option.isSome o) |> Array.map Option.get
        return new ApplicationUser(
            Id=id,
            UserName=userName,
            PasswordHash=passwordHash,
            Claims=claims.ToList())
    }

let findUserByIdAsync (connString : string) (id : int) : Async<ApplicationUser option> =
    let userResponseMapper = userResponseMapper connString
    queryRecord connString "SELECT * FROM users.user WHERE id = @id;" (Map.ofList ["id", id :> obj]) userResponseMapper

let findUserByNameAsync (connString : string) (name : string) : Async<ApplicationUser option> =
    let userResponseMapper = userResponseMapper connString
    queryRecord connString "SELECT * FROM users.user WHERE user_name = @user_name;" (Map.ofList ["user_name", name :> obj]) userResponseMapper

let getAllUsers (connString : string) : Async<ApplicationUser[]> =
    async {
        let! userIds = queryRecordSet<int> connString "SELECT id FROM users.user;" Map.empty
        let! users = userIds |> Array.map (findUserByIdAsync connString) |> Async.Sequential
        return users |> Array.filter Option.isSome |> Array.map Option.get
    }
