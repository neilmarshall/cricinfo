CREATE SCHEMA IF NOT EXISTS users;

CREATE TABLE IF NOT EXISTS users.user (
    id SERIAL PRIMARY KEY,
    user_name VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL);

CREATE TABLE IF NOT EXISTS users.claim (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users.user(id) NOT NULL,
    claim_type VARCHAR(255) NOT NULL,
    claim_value VARCHAR(255) NOT NULL);
CREATE UNIQUE INDEX ON users.claim (user_id, lower(claim_type));

CREATE OR REPLACE FUNCTION users.create_user(VARCHAR(255), VARCHAR(255))
RETURNS int
AS $$
BEGIN
    INSERT INTO users.user (user_name, password_hash) VALUES ($1, $2);
    RETURN (SELECT id FROM users.user WHERE user_name = $1);
END; $$
LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION users.delete_user_by_id(INT)
RETURNS void
AS $$
BEGIN
    DELETE FROM users.claim WHERE user_id = $1;
    DELETE FROM users.user WHERE id = $1;
END; $$
LANGUAGE plpgsql;

CREATE OR REPLACE VIEW users.user_claims
AS
    SELECT u.id AS user_id, u.user_name, c.id AS claim_id, c.claim_type, c.claim_value
      FROM users.user u
      LEFT JOIN users.claim c
        ON u.id = c.user_id
     ORDER BY u.user_name, c.claim_type;
