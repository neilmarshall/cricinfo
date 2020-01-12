DROP TABLE IF EXISTS fall_of_wicket_scorecard;
DROP TABLE IF EXISTS innings;
DROP TABLE IF EXISTS squad;
DROP TABLE IF EXISTS bowling_scorecard;
DROP TABLE IF EXISTS batting_scorecard;
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

CREATE TABLE venue (
    id SERIAL PRIMARY KEY,
	name VARCHAR(255) NOT NULL UNIQUE
);

CREATE TABLE result (
    id SERIAL PRIMARY KEY,
	type VARCHAR(255) NOT NULL UNIQUE
);
INSERT INTO result (type) VALUES ('Home Team Win'), ('Away Team Win'), ('Draw');

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

CREATE TABLE how_out (
    id SERIAL PRIMARY KEY,
	type VARCHAR(255) UNIQUE
);
INSERT INTO how_out (type) VALUES ('not out'), ('c'), ('b'), ('lbw');
	
CREATE TABLE batting_scorecard (
    id SERIAL PRIMARY KEY,
	batsman_id INT REFERENCES player(id) NOT NULL,
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
	bowler_id INT REFERENCES player(id) NOT NULL,
	overs INT NOT NULL DEFAULT 0,
	maidens INT NOT NULL DEFAULT 0,
	runs_conceded INT NOT NULL DEFAULT 0,
    wickets INT NOT NULL DEFAULT 0,
	CONSTRAINT overs_non_negative CHECK (overs >= 0),
	CONSTRAINT maidens_non_negative CHECK (maidens >= 0),
	CONSTRAINT runs_conceded_non_negative CHECK (runs_conceded >= 0),
	CONSTRAINT wickets_non_negative CHECK (wickets >= 0)
);

CREATE TABLE squad (
    id SERIAL PRIMARY KEY,
	match_id INT REFERENCES match(id) NOT NULL,
	team_id INT REFERENCES team(id) NOT NULL,
	player_id INT REFERENCES player(id) NOT NULL,
	batting_order INT,
	UNIQUE(match_id, team_id, player_id)
);

CREATE TABLE innings (
    id SERIAL PRIMARY KEY,
	match_id INT REFERENCES match(id) NOT NULL,
    team_id INT REFERENCES team(id) NOT NULL,
	innings INT NOT NULL,
	batting_scorecard_id INT REFERENCES batting_scorecard(id),
	bowling_scorecard_id INT REFERENCES bowling_scorecard(id),
	UNIQUE (match_id, team_id, innings, batting_scorecard_id, bowling_scorecard_id)
);

CREATE TABLE fall_of_wicket_scorecard (
	innings_id INT NOT NULL REFERENCES innings(id),
	runs INT NOT NULL DEFAULT 0,
	wickets INT NOT NULL DEFAULT 0,
	PRIMARY KEY (innings_id, wickets),
	CONSTRAINT runs_non_negative CHECK (runs >= 0),
	CONSTRAINT wickets_non_negative CHECK (wickets >= 0)
);
