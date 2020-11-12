namespace Cricinfo.Services.IdentityStore.Models

open Microsoft.AspNetCore.Identity

[<AllowNullLiteral>]
type public ApplicationUser() =
    inherit IdentityUser<int>()
    member val Claims = new ResizeArray<IdentityUserClaim<int>>() with get, set
