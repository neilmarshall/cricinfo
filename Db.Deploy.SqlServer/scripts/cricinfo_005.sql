CREATE VIEW show_matches
AS
    SELECT m.id AS 'Id', mt.type AS 'Match Type',
        m.date_of_first_day AS 'Date', ht.name AS 'Home Team',
        at.name AS 'Away Team', r.type AS 'Result'
    FROM match m
    JOIN match_type mt
    ON m.match_type_id = mt.id
    JOIN venue v
    ON m.venue_id = v.id
    JOIN team ht
    ON m.hometeam_id = ht.id
    JOIN team at
    ON m.awayteam_id = at.id
    JOIN result r
    ON m.result_id = r.id;