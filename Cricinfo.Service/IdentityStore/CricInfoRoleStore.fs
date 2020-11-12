namespace Cricinfo.Services.IdentityStore

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Identity
open Cricinfo.Services.IdentityStore.Models

type public CricInfoRoleStore() =
    interface IRoleStore<ApplicationRole> with
        member this.CreateAsync(role : ApplicationRole, cancellationToken : CancellationToken) : Task<IdentityResult> =
            raise <| NotImplementedException()

        member this.DeleteAsync(role : ApplicationRole, cancellationToken : CancellationToken) : Task<IdentityResult> =
            raise <| NotImplementedException()

        member this.FindByIdAsync(id : string, cancellationToken : CancellationToken) : Task<ApplicationRole> =
            raise <| NotImplementedException()

        member this.FindByNameAsync(name : string, cancellationToken : CancellationToken) : Task<ApplicationRole> =
            raise <| NotImplementedException()

        member this.GetNormalizedRoleNameAsync(role : ApplicationRole, cancellationToken : CancellationToken) : Task<string> =
            raise <| NotImplementedException()

        member this.GetRoleIdAsync(role : ApplicationRole, cancellationToken : CancellationToken) : Task<string> =
            raise <| NotImplementedException()

        member this.GetRoleNameAsync(role : ApplicationRole, cancellationToken : CancellationToken) : Task<string> =
            raise <| NotImplementedException()

        member this.SetNormalizedRoleNameAsync(role : ApplicationRole, roleName : string, cancellationToken : CancellationToken) : Task =
            raise <| NotImplementedException()

        member this.SetRoleNameAsync(role : ApplicationRole, roleName : string, cancellationToken : CancellationToken) : Task =
            raise <| NotImplementedException()

        member this.UpdateAsync(role : ApplicationRole, cancellationToken : CancellationToken) : Task<IdentityResult> =
            raise <| NotImplementedException()

        member this.Dispose() : unit = ()
