CREATE SCHEMA IF NOT EXISTS matchdata;

ALTER TABLE batting_scorecard SET SCHEMA matchdata;
ALTER TABLE bowling_scorecard SET SCHEMA matchdata;
ALTER TABLE fall_of_wicket_scorecard SET SCHEMA matchdata;
ALTER TABLE how_out SET SCHEMA matchdata;
ALTER TABLE innings SET SCHEMA matchdata;
ALTER TABLE match SET SCHEMA matchdata;
ALTER TABLE match_type SET SCHEMA matchdata;
ALTER TABLE player SET SCHEMA matchdata;
ALTER TABLE result SET SCHEMA matchdata;
ALTER TABLE squad SET SCHEMA matchdata;
ALTER TABLE team SET SCHEMA matchdata;
ALTER TABLE venue SET SCHEMA matchdata;

ALTER VIEW show_matches SET SCHEMA matchdata;

ALTER FUNCTION check_match_exists SET SCHEMA matchdata;
ALTER FUNCTION delete_match(INTEGER) SET SCHEMA matchdata;
ALTER FUNCTION delete_match(TEXT, TEXT, TIMESTAMP) SET SCHEMA matchdata;
ALTER FUNCTION get_batting_info SET SCHEMA matchdata;
ALTER FUNCTION get_bowling_info SET SCHEMA matchdata;
ALTER FUNCTION get_id_and_insert_if_not_exists_player SET SCHEMA matchdata;
ALTER FUNCTION get_id_and_insert_if_not_exists_team SET SCHEMA matchdata;
ALTER FUNCTION get_id_and_insert_if_not_exists_venue SET SCHEMA matchdata;
ALTER FUNCTION get_match_info SET SCHEMA matchdata;
ALTER FUNCTION get_scorecard_info SET SCHEMA matchdata;

CREATE OR REPLACE FUNCTION matchdata.get_id_and_insert_if_not_exists_team (VARCHAR(255)) RETURNS integer
AS $$
BEGIN
    IF NOT (SELECT EXISTS (SELECT 1 FROM matchdata.team WHERE lower(name) = lower($1))) THEN
        INSERT INTO matchdata.team (name) VALUES ($1);
    END IF;
    RETURN (SELECT id FROM matchdata.team WHERE lower(name) = lower($1));
END; $$
LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION matchdata.get_id_and_insert_if_not_exists_venue (VARCHAR(255)) RETURNS integer
AS $$
BEGIN
    IF NOT (SELECT EXISTS (SELECT 1 FROM matchdata.venue WHERE lower(name) = lower($1))) THEN
        INSERT INTO matchdata.venue (name) VALUES ($1);
    END IF;
    RETURN (SELECT id FROM matchdata.venue WHERE lower(name) = lower($1));
END; $$
LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION matchdata.get_id_and_insert_if_not_exists_player (VARCHAR(255), VARCHAR(255)) RETURNS integer
AS $$
BEGIN
    IF NOT (SELECT EXISTS (SELECT 1 FROM matchdata.player WHERE lower(firstname) = lower($1) AND lower(lastname) = lower($2))) THEN
        INSERT INTO matchdata.player (firstname, lastname) VALUES ($1, $2);
    END IF;
    RETURN (SELECT id FROM matchdata.player WHERE lower(firstname) = lower($1) AND lower(lastname) = lower($2));
END; $$
LANGUAGE PLPGSQL;

CREATE OR REPLACE FUNCTION matchdata.check_match_exists (TEXT, TEXT, TIMESTAMP)
RETURNS bool
AS $$
DECLARE
    home_team_id INT;
    away_team_id INT;
BEGIN
    SELECT id INTO home_team_id FROM matchdata.team WHERE lower(name) = lower($1);
    SELECT id INTO away_team_id FROM matchdata.team WHERE lower(name) = lower($2);
    RETURN (SELECT EXISTS (SELECT 1 FROM matchdata.match WHERE home_team_id = home_team_id AND away_team_id = away_team_id AND date_of_first_day = $3));
END; $$
LANGUAGE PLPGSQL;

CREATE OR REPLACE FUNCTION matchdata.delete_match(INT)
RETURNS VOID
AS $$
BEGIN
    DELETE FROM matchdata.match WHERE id = $1;
END; $$
LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION matchdata.delete_match(TEXT, TEXT, TIMESTAMP)
RETURNS VOID
AS $$
DECLARE
    home_team_id INT;
    away_team_id INT;
BEGIN
    SELECT id INTO home_team_id FROM matchdata.team WHERE lower(name) = lower($1);
    SELECT id INTO away_team_id FROM matchdata.team WHERE lower(name) = lower($2);
    DELETE FROM matchdata.match WHERE home_team_id = home_team_id AND away_team_id = away_team_id AND date_of_first_day = $3;
END; $$
LANGUAGE PLPGSQL;

CREATE OR REPLACE FUNCTION matchdata.get_match_info(INT)
RETURNS TABLE(
    venue VARCHAR(255),
    match_type_id VARCHAR(255),
    date DATE,
    home_team VARCHAR(255),
    away_team VARCHAR(255),
    result VARCHAR(255),
    home_squad TEXT[],
    away_squad TEXT[]
)
LANGUAGE plpgsql
AS $$
DECLARE
    home_squad TEXT[];
    away_squad TEXT[];
BEGIN
    home_squad := Array((SELECT CONCAT(p.firstname, ' ', p.lastname) FROM matchdata.squad s JOIN matchdata.player p ON s.player_id = p.id WHERE s.match_id = $1 AND s.team_id = (SELECT hometeam_id FROM matchdata.match WHERE id = $1)));
    away_squad := Array((SELECT CONCAT(p.firstname, ' ', p.lastname) FROM matchdata.squad s JOIN matchdata.player p ON s.player_id = p.id WHERE s.match_id = $1 AND s.team_id = (SELECT awayteam_id FROM matchdata.match WHERE id = $1)));
    RETURN QUERY
    SELECT v.name, mt.type, m.date_of_first_day, ht.name, at.name, r.type, home_squad, away_squad
      FROM matchdata.match m
      JOIN matchdata.venue v
        ON m.venue_id = v.id
      JOIN matchdata.match_type mt
        ON m.match_type_id = mt.id
      JOIN matchdata.team ht
        ON m.hometeam_id = ht.id
      JOIN matchdata.team at
        ON m.awayteam_id = at.id
      JOIN matchdata.result r
        ON m.result_id = r.id
     WHERE m.id = $1;
END
$$;

CREATE OR REPLACE FUNCTION matchdata.get_scorecard_info(INT)
RETURNS TABLE(
    team VARCHAR(255),
    innings INT,
    extras INT,
    declared BOOLEAN,
    fall_of_wicket_scorecard INT[]
)
LANGUAGE plpgsql
AS $$
DECLARE
    fall_of_wicket_scorecard INT[];
BEGIN
    fall_of_wicket_scorecard := Array(SELECT runs FROM matchdata.fall_of_wicket_scorecard WHERE innings_id = $1);
    RETURN QUERY
    SELECT t.name, i.innings, i.extras, i.declared, fall_of_wicket_scorecard
      FROM matchdata.innings i
      JOIN matchdata.team t
        ON i.team_id = t.id
     WHERE i.id = $1;
END
$$;

CREATE OR REPLACE FUNCTION matchdata.get_batting_info(INT)
RETURNS TABLE(
    name TEXT,
    dismissal VARCHAR(255),
    catcher TEXT,
    bowler TEXT,
    runs INT,
    mins INT,
    balls INT,
    fours INT,
    sixes INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT CONCAT(p.firstname, ' ', p.lastname), h.type, CONCAT(c.firstname, ' ', c.lastname), CONCAT(b.firstname, ' ', b.lastname), s.runs, s.mins, s.balls, s.fours, s.sixes
      FROM matchdata.batting_scorecard s
      JOIN matchdata.player p
        ON s.batsman_id = p.id
      JOIN matchdata.how_out h
        ON s.how_out_id = h.id
      LEFT JOIN matchdata.player c
        ON c.id = s.catcher_id
      LEFT JOIN matchdata.player b
        ON b.id = s.bowler_id
     WHERE s.id = $1;
END
$$;

CREATE OR REPLACE FUNCTION matchdata.get_bowling_info(INT)
RETURNS TABLE(
    name TEXT,
    overs NUMERIC(3, 1),
    maidens INT,
    runs INT,
    wickets INT
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT CONCAT(p.firstname, ' ', p.lastname), s.overs, s.maidens, s.runs_conceded, s.wickets
      FROM matchdata.bowling_scorecard s
      JOIN matchdata.player p
        ON s.bowler_id = p.id
     WHERE s.id = $1;
END
$$;
