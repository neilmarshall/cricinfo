-- Create stored procedures to check if match data exists

CREATE PROCEDURE check_match_exists
    @home_team_name VARCHAR(MAX),
    @away_team_name VARCHAR(MAX),
    @date_of_first_day DATE,
    @exists BIT OUTPUT
AS
BEGIN
    DECLARE @home_team_id INT, @away_team_id INT
    SELECT @home_team_id = id FROM team WHERE lower(name) = lower(@home_team_name)
    SELECT @away_team_id = id FROM team WHERE lower(name) = lower(@away_team_name)

    IF EXISTS (SELECT * FROM match WHERE hometeam_id = @home_team_id
               AND awayteam_id = @away_team_id
               AND date_of_first_day = @date_of_first_day)
		BEGIN
			SET @exists = 1
		END
    ELSE
		BEGIN
			SET @exists = 0
		END
END