-- Create stored procedures to insert and retrieve IDs for team / venue / player

CREATE OR REPLACE FUNCTION get_id_and_insert_if_not_exists_team (VARCHAR(255)) RETURNS integer
AS $$
BEGIN
    IF NOT (SELECT EXISTS (SELECT 1 FROM team WHERE lower(name) = lower($1))) THEN
        INSERT INTO team (name) VALUES ($1);
    END IF;
    RETURN (SELECT id FROM team WHERE lower(name) = lower($1));
END; $$
LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION get_id_and_insert_if_not_exists_venue (VARCHAR(255)) RETURNS integer
AS $$
BEGIN
    IF NOT (SELECT EXISTS (SELECT 1 FROM venue WHERE lower(name) = lower($1))) THEN
        INSERT INTO venue (name) VALUES ($1);
    END IF;
    RETURN (SELECT id FROM venue WHERE lower(name) = lower($1));
END; $$
LANGUAGE PLPGSQL;


CREATE OR REPLACE FUNCTION get_id_and_insert_if_not_exists_player (VARCHAR(255), VARCHAR(255)) RETURNS integer
AS $$
BEGIN
    IF NOT (SELECT EXISTS (SELECT 1 FROM player WHERE lower(firstname) = lower($1) AND lower(lastname) = lower($2))) THEN
        INSERT INTO player (firstname, lastname) VALUES ($1, $2);
    END IF;
    RETURN (SELECT id FROM player WHERE lower(firstname) = lower($1) AND lower(lastname) = lower($2));
END; $$
LANGUAGE PLPGSQL;
