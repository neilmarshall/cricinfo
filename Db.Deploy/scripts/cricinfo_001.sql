-- Create table and relations

DROP TABLE IF EXISTS fall_of_wicket_scorecard;
DROP TABLE IF EXISTS bowling_scorecard;
DROP TABLE IF EXISTS batting_scorecard;
DROP TABLE IF EXISTS innings;
DROP TABLE IF EXISTS squad;
DROP TABLE IF EXISTS how_out;
DROP TABLE IF EXISTS player;
DROP TABLE IF EXISTS match;
DROP TABLE IF EXISTS result;
DROP TABLE IF EXISTS venue;
DROP TABLE IF EXISTS team;

CREATE TABLE team (
    id SERIAL PRIMARY KEY,
	name VARCHAR(255) NOT NULL UNIQUE
);
CREATE UNIQUE INDEX ON team (lower(name));

CREATE TABLE venue (
    id SERIAL PRIMARY KEY,
	name VARCHAR(255) NOT NULL UNIQUE
);
CREATE UNIQUE INDEX ON venue (lower(name));

CREATE TABLE result (
    id SERIAL PRIMARY KEY,
	type VARCHAR(255) NOT NULL UNIQUE
);
INSERT INTO result (type) VALUES ('HomeTeamWin'), ('AwayTeamWin'), ('Draw'), ('Tie');

CREATE TABLE match (
    id SERIAL PRIMARY KEY,
	date_of_first_day DATE NOT NULL,
	venue_id INT REFERENCES venue(id) NOT NULL,
    hometeam_id INT REFERENCES team(id) NOT NULL,
	awayteam_id INT REFERENCES team(id) NOT NULL,
	result_id INT REFERENCES result(id) NOT NULL
);

CREATE TABLE player (
    id SERIAL PRIMARY KEY,
	firstname VARCHAR(255) NOT NULL,
	lastname VARCHAR(255) NOT NULL,
	UNIQUE (firstname, lastname)
);
CREATE UNIQUE INDEX ON player (lower(firstname), lower(lastname));

CREATE TABLE squad (
    id SERIAL PRIMARY KEY,
	match_id INT NOT NULL REFERENCES match(id) ON DELETE CASCADE,
	team_id INT NOT NULL REFERENCES team(id),
	player_id INT NOT NULL REFERENCES player(id),
	UNIQUE(match_id, team_id, player_id)
);

CREATE TABLE how_out (
    id SERIAL PRIMARY KEY,
	type VARCHAR(255) UNIQUE
);
INSERT INTO how_out (type) VALUES ('Caught'), ('Bowled'), ('CaughtAndBowled'), ('LBW'), ('NotOut'), ('RunOut');

CREATE TABLE innings (
    id SERIAL PRIMARY KEY,
	match_id INT NOT NULL REFERENCES match(id) ON DELETE CASCADE,
    team_id INT NOT NULL REFERENCES team(id),
	innings INT NOT NULL,
	extras INT NOT NULL,
	UNIQUE (match_id, team_id, innings)
);
	
CREATE TABLE batting_scorecard (
    id SERIAL PRIMARY KEY,
	innings_id INT NOT NULL REFERENCES innings(id) ON DELETE CASCADE,
	batsman_id INT NOT NULL REFERENCES player(id),
	how_out_id INT REFERENCES how_out(id),
	catcher_id INT REFERENCES player(id),
	bowler_id INT REFERENCES player(id),
	runs INT NOT NULL DEFAULT 0,
	mins INT NOT NULL DEFAULT 0,
	balls INT NOT NULL DEFAULT 0,
	fours INT NOT NULL DEFAULT 0,
	sixes INT NOT NULL DEFAULT 0,
	CONSTRAINT runs_non_negative CHECK (runs >= 0),
	CONSTRAINT mins_non_negative CHECK (mins >= 0),
	CONSTRAINT balls_non_negative CHECK (balls >= 0),
	CONSTRAINT fours_non_negative CHECK (fours >= 0),
	CONSTRAINT sixes_non_negative CHECK (sixes >= 0)
);

CREATE TABLE bowling_scorecard (
    id SERIAL PRIMARY KEY,
	innings_id INT NOT NULL REFERENCES innings(id) ON DELETE CASCADE,
	bowler_id INT NOT NULL REFERENCES player(id),
	overs INT NOT NULL DEFAULT 0,
	maidens INT NOT NULL DEFAULT 0,
	runs_conceded INT NOT NULL DEFAULT 0,
    wickets INT NOT NULL DEFAULT 0,
	CONSTRAINT overs_non_negative CHECK (overs >= 0),
	CONSTRAINT maidens_non_negative CHECK (maidens >= 0),
	CONSTRAINT runs_conceded_non_negative CHECK (runs_conceded >= 0),
	CONSTRAINT wickets_non_negative CHECK (wickets >= 0)
);

CREATE TABLE fall_of_wicket_scorecard (
	innings_id INT NOT NULL REFERENCES innings(id) ON DELETE CASCADE,
	runs INT NOT NULL DEFAULT 0,
	wickets INT NOT NULL DEFAULT 0,
	PRIMARY KEY (innings_id, wickets),
	CONSTRAINT runs_non_negative CHECK (runs >= 0),
	CONSTRAINT wickets_non_negative CHECK (wickets >= 0)
);
