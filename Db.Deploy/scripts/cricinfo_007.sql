CREATE OR REPLACE FUNCTION get_match_info(INT)
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
    home_squad := Array((SELECT CONCAT(p.firstname, ' ', p.lastname) FROM squad s JOIN player p ON s.player_id = p.id WHERE s.match_id = $1 AND s.team_id = (SELECT hometeam_id FROM match WHERE id = $1)));
    away_squad := Array((SELECT CONCAT(p.firstname, ' ', p.lastname) FROM squad s JOIN player p ON s.player_id = p.id WHERE s.match_id = $1 AND s.team_id = (SELECT awayteam_id FROM match WHERE id = $1)));
    RETURN QUERY
    SELECT v.name, mt.type, m.date_of_first_day, ht.name, at.name, r.type, home_squad, away_squad
      FROM match m
      JOIN venue v
        ON m.venue_id = v.id
      JOIN match_type mt
        ON m.match_type_id = mt.id
      JOIN team ht
        ON m.hometeam_id = ht.id
      JOIN team at
        ON m.awayteam_id = at.id
      JOIN result r
        ON m.result_id = r.id
     WHERE m.id = $1;
END
$$;


CREATE OR REPLACE FUNCTION get_scorecard_info(INT)
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
    fall_of_wicket_scorecard := Array(SELECT runs FROM fall_of_wicket_scorecard WHERE innings_id = $1);
    RETURN QUERY
    SELECT t.name, i.innings, i.extras, i.declared, fall_of_wicket_scorecard
      FROM innings i
      JOIN team t
        ON i.team_id = t.id
     WHERE i.id = $1;
END
$$;


CREATE OR REPLACE FUNCTION get_batting_info(INT)
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
      FROM batting_scorecard s
      JOIN player p
        ON s.batsman_id = p.id
      JOIN how_out h
        ON s.how_out_id = h.id
      LEFT JOIN player c
        ON c.id = s.catcher_id
      LEFT JOIN player b
        ON b.id = s.bowler_id
     WHERE s.id = $1;
END
$$;

CREATE OR REPLACE FUNCTION get_bowling_info(INT)
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
      FROM bowling_scorecard s
      JOIN player p
        ON s.bowler_id = p.id
     WHERE s.id = $1;
END
$$;
