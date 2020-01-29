-- Create stored procedures to show all match data

CREATE OR REPLACE FUNCTION show_matches()
RETURNS TABLE ("Id" INTEGER, "Match Type" VARCHAR(255), "Date" Date, "Home Team" VARCHAR(255), "Away Team" VARCHAR(255), Result VARCHAR(255))
AS $$
BEGIN
    RETURN QUERY SELECT m.id, mt.type, m.date_of_first_day, ht.name, at.name, r.type
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
END; $$
LANGUAGE PLPGSQL;
