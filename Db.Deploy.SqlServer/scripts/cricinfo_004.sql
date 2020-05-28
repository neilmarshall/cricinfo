-- Create stored procedures to delete match data

CREATE PROCEDURE delete_match_by_id @matchid INT
AS
BEGIN
    DELETE FROM match WHERE id = @matchid
END
GO


CREATE PROCEDURE delete_match
    @home_team_name VARCHAR(255),
    @away_team_name VARCHAR(255),
    @date_of_first_day DATE
AS
BEGIN
    DECLARE @home_team_id INT, @away_team_id INT
    SELECT @home_team_id = id FROM team WHERE lower(name) = lower(@home_team_name)
    SELECT @away_team_id = id FROM team WHERE lower(name) = lower(@away_team_name)
    DELETE FROM match
     WHERE hometeam_id = @home_team_id
       AND awayteam_id = @away_team_id
       AND date_of_first_day = @date_of_first_day
END
GO