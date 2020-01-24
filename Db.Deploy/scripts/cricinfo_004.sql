-- Create stored procedures to delete match data

CREATE OR REPLACE FUNCTION delete_match(INT)
RETURNS VOID
AS $$
BEGIN
    DELETE FROM match WHERE id = $1;
END; $$
LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION delete_match(TEXT, TEXT, TIMESTAMP)
RETURNS VOID
AS $$
DECLARE
    home_team_id INT;
    away_team_id INT;
BEGIN
    SELECT id INTO home_team_id FROM team WHERE name = $1;
    SELECT id INTO away_team_id FROM team WHERE name = $2;
    DELETE FROM match WHERE home_team_id = home_team_id AND away_team_id = away_team_id AND date_of_first_day = $3;
END; $$
LANGUAGE PLPGSQL;
