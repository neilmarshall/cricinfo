namespace Cricinfo.Services.IdentityStore.Models

open System

type public ApplicationRole() =
    inherit Microsoft.AspNetCore.Identity.IdentityRole<Guid>()
