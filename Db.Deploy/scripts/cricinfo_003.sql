-- Create stored procedures to check if match data exists

CREATE OR REPLACE FUNCTION check_match_exists (TEXT, TEXT, TIMESTAMP)
RETURNS bool
AS $$
DECLARE
    home_team_id INT;
    away_team_id INT;
BEGIN
    SELECT id INTO home_team_id FROM team WHERE name = $1;
    SELECT id INTO away_team_id FROM team WHERE name = $2;
    RETURN (SELECT EXISTS (SELECT 1 FROM match WHERE home_team_id = home_team_id AND away_team_id = away_team_id AND date_of_first_day = $3));
END; $$
LANGUAGE PLPGSQL;