-- Create stored procedures to show all match data

CREATE OR REPLACE FUNCTION show_matches()
RETURNS TABLE ("Id" INTEGER, "Date" Date, "Home Team" VARCHAR(255), "Away Team" VARCHAR(255), Result VARCHAR(255))
AS $$
BEGIN
    RETURN QUERY SELECT m.id, m.date_of_first_day, ht.name, at.name, r.type
    FROM match m
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
