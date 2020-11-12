namespace Cricinfo.Services.IdentityStore

open System
open System.Collections.Generic
open System.Linq
open System.Security.Claims
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.Logging
open Cricinfo.Services.IdentityStore.Models
open PostgresDbCommandFunctions
open PostgresDbQueryFunctions

type public CricInfoUserStore<'T>(connString : string, logger : ILogger<'T>) =

    new (connString : string) =
        new CricInfoUserStore<'T>(connString, null)

    interface IUserStore<ApplicationUser> with

        member this.CreateAsync(user : ApplicationUser, cancellationToken : CancellationToken) : Task<IdentityResult> =
            let createUser() =
                async {
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    do! createUserAsync conn trans user
                    trans.Commit()
                    return IdentityResult.Success
                }
            genericQueryWrapper logger createUser <| IdentityResult.Failed()

        member this.DeleteAsync(user : ApplicationUser, cancellationToken : CancellationToken) : Task<IdentityResult> =
            let deleteUser() =
                async {
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    do! deleteUserAsync conn trans user
                    trans.Commit()
                    return IdentityResult.Success
                }
            genericQueryWrapper logger deleteUser <| IdentityResult.Failed()

        member this.FindByIdAsync(id : string, cancellationToken : CancellationToken) : Task<ApplicationUser> =
            let findUserById() =
                async {
                    let! user = findUserByIdAsync connString (id |> int)
                    return if Option.isSome user then Option.get user else null
                }
            genericQueryWrapper logger findUserById null

        member this.FindByNameAsync(name : string, cancellationToken : CancellationToken) : Task<ApplicationUser> =
            let findUserByName() =
                async {
                    let! user = findUserByNameAsync connString name
                    return if Option.isSome user then Option.get user else null
                }
            genericQueryWrapper logger findUserByName null

        member this.GetNormalizedUserNameAsync(user : ApplicationUser, cancellationToken : CancellationToken) : Task<string> =
            raise <| NotImplementedException()

        member this.GetUserIdAsync(user : ApplicationUser, cancellationToken : CancellationToken) : Task<string> =
            user.Id.ToString() |> Task.FromResult

        member this.GetUserNameAsync(user : ApplicationUser, cancellationToken : CancellationToken) : Task<string> =
            user.UserName |> Task.FromResult

        member this.SetNormalizedUserNameAsync(user : ApplicationUser, userName : string, cancellationToken : CancellationToken) : Task =
            user.UserName <- userName
            Task.CompletedTask

        member this.SetUserNameAsync(user : ApplicationUser, userName : string, cancellationToken : CancellationToken) : Task =
            raise <| NotImplementedException()

        member this.UpdateAsync(user : ApplicationUser, cancellationToken : CancellationToken) : Task<IdentityResult> =
            let updateUser() =
                async {
                    use conn = getConnection connString
                    do! conn.OpenAsync() |> Async.AwaitTask
                    use trans = conn.BeginTransaction()
                    do! updateUserAsync conn trans user
                    trans.Commit()
                    return IdentityResult.Success
                }
            genericQueryWrapper logger updateUser <| IdentityResult.Failed()

        member this.Dispose() : unit = ()

    interface IUserPasswordStore<ApplicationUser> with

        member this.GetPasswordHashAsync(user: ApplicationUser, cancellationToken: CancellationToken): Task<string> =
            user.PasswordHash |> Task.FromResult

        member this.HasPasswordAsync(user: ApplicationUser, cancellationToken: CancellationToken): Task<bool> =
            user.PasswordHash <> null |> Task.FromResult

        member this.SetPasswordHashAsync(user: ApplicationUser, passwordHash: string, cancellationToken: CancellationToken): Task =
            user.PasswordHash <- passwordHash
            Task.CompletedTask

    interface IUserClaimStore<ApplicationUser> with

        member this.GetClaimsAsync(user: ApplicationUser, cancellationToken: CancellationToken) : Task<IList<Claim>> =
            user.Claims.Select(fun c -> new Claim(c.ClaimType, c.ClaimValue)).ToList() :> IList<Claim> |> Task.FromResult

        member this.AddClaimsAsync(user: ApplicationUser, claims: IEnumerable<Claim>, cancellationToken: CancellationToken) : Task =
            for claim in claims do
                let claim' = new IdentityUserClaim<int>(UserId=user.Id)
                claim'.InitializeFromClaim(claim)
                user.Claims.Add(claim')
            Task.CompletedTask

        member this.ReplaceClaimAsync(user: ApplicationUser, claim: Claim, newClaim: Claim, cancellationToken: CancellationToken) : Task =
            user.Claims.First(fun c -> c.ClaimType = claim.Type).ClaimValue <- newClaim.Value
            Task.CompletedTask

        member this.RemoveClaimsAsync(user: ApplicationUser, claims: IEnumerable<Claim>, cancellationToken: CancellationToken) : Task =
            raise <| NotImplementedException()

        member this.GetUsersForClaimAsync(claim: Claim, cancellationToken: CancellationToken) : Task<IList<ApplicationUser>> =
            raise <| NotImplementedException()

    interface IQueryableUserStore<ApplicationUser> with
        member this.Users with get() : IQueryable<ApplicationUser> = (connString |> getAllUsers |> Async.RunSynchronously).AsQueryable()
