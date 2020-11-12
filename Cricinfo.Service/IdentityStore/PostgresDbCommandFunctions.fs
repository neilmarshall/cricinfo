namespace Cricinfo.Services.IdentityStore

module private PostgresDbCommandFunctions =

    open Npgsql
    open Cricinfo.Services.IdentityStore.Models
    open Cricinfo.Services.PostgresDbCommandFunctions

    let getConnection = getConnection

    let createUserAsync
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (user : ApplicationUser)
            : Async<Unit> =
        async {
            let query = "SELECT * FROM users.create_user(@user_name, @password_hash);"
            let parameters = Map.ofList [ "user_name", box user.UserName; "password_hash", box user.PasswordHash ]
            let! id = executeScalarAsync<int> conn trans query parameters
            if Seq.length user.Claims > 0 then
                for claim in user.Claims do
                    let query = "INSERT INTO users.claim (user_id, claim_type, claim_value) VALUES (@user_id, @claim_type, @claim_value);"
                    let parameters = Map.ofList [ "user_id", box id; "claim_type", box claim.ClaimType; "claim_value", box claim.ClaimValue ]
                    executeNonQuery conn trans query parameters
        }

    let updateUserAsync
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (user : ApplicationUser)
            : Async<Unit> =
        async {
            let query = "UPDATE users.user SET (user_name, password_hash) = (@user_name, @password_hash) WHERE id = @id;"
            let parameters = Map.ofList [ "id", box user.Id; "user_name", box user.UserName; "password_hash", box user.PasswordHash ]
            executeNonQuery conn trans query parameters
            executeNonQuery conn trans "DELETE FROM users.claim WHERE user_id = @user_id;" <| Map.ofList [ "user_id", box user.Id ]
            if Seq.length user.Claims > 0 then
                for claim in user.Claims do
                    let query = "INSERT INTO users.claim (user_id, claim_type, claim_value) VALUES (@user_id, @claim_type, @claim_value);"
                    let parameters = Map.ofList [ "user_id", box user.Id; "claim_type", box claim.ClaimType; "claim_value", box claim.ClaimValue ]
                    executeNonQuery conn trans query parameters
            return ()
        }

    let deleteUserAsync
        (conn : NpgsqlConnection)
        (trans : NpgsqlTransaction)
        (user : ApplicationUser)
            : Async<Unit> =
        async {
            let query = "SELECT FROM users.delete_user_by_id(@id);"
            let parameters = Map.ofList [ "id", box user.Id ]
            executeNonQuery conn trans query parameters
        }
