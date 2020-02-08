-- Create view to show all match data

CREATE OR REPLACE VIEW show_matches ("Id", "Match Type", "Date", "Home Team", "Away Team", "Result")
AS
    SELECT m.id, mt.type, m.date_of_first_day, ht.name, at.name, r.type
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
    ON m.result_id = r.id
    ORDER BY m.date_of_first_day;
