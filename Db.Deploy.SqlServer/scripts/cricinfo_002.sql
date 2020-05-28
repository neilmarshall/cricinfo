-- Create stored procedures to insert and retrieve IDs for team / venue / player

CREATE PROCEDURE get_id_and_insert_if_not_exists_team
    @team VARCHAR(255),
	@id INT OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM team WHERE lower(name) = lower(@team))
		BEGIN
			INSERT INTO team (name) VALUES (@team)
		END
    SELECT @id=id FROM team WHERE lower(name) = lower(@team)
END
GO

CREATE PROCEDURE get_id_and_insert_if_not_exists_venue
    @venue VARCHAR(255),
	@id INT OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM venue WHERE lower(name) = lower(@venue))
		BEGIN
			INSERT INTO venue (name) VALUES (@venue)
		END
    SELECT @id=id FROM venue WHERE lower(name) = lower(@venue)
END
GO

CREATE PROCEDURE get_id_and_insert_if_not_exists_player
    @firstname VARCHAR(255),
    @lastname VARCHAR(255),
    @id INT OUTPUT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM player WHERE lower(firstname) = lower(@firstname) AND lower(lastname) = lower(@lastname))
		BEGIN
			INSERT INTO player (firstname, lastname) VALUES (@firstname, @lastname)
		END
    SELECT @id=id FROM player WHERE lower(firstname) = lower(@firstname) AND lower(lastname) = lower(@lastname)
END
GO
