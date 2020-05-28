-- Create table and relations

CREATE TABLE team (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	name VARCHAR(255) NOT NULL UNIQUE
);

CREATE TABLE venue (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	name VARCHAR(255) NOT NULL UNIQUE
);

CREATE TABLE result (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	type VARCHAR(255) NOT NULL UNIQUE
);
INSERT INTO result (type) VALUES ('HomeTeamWin'), ('AwayTeamWin'), ('Draw'), ('Tie');

CREATE TABLE match_type (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	type VARCHAR(255) NOT NULL UNIQUE
);
INSERT INTO match_type (type) VALUES ('TestMatch'), ('ODI'), ('T20');

CREATE TABLE match (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
    match_type_id INT REFERENCES match_type(id) NOT NULL,
	date_of_first_day DATE NOT NULL,
	venue_id INT REFERENCES venue(id) NOT NULL,
    hometeam_id INT REFERENCES team(id) NOT NULL,
	awayteam_id INT REFERENCES team(id) NOT NULL,
	result_id INT REFERENCES result(id) NOT NULL
);

CREATE TABLE player (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	firstname VARCHAR(255) NOT NULL,
	lastname VARCHAR(255) NOT NULL,
	UNIQUE (firstname, lastname)
);

CREATE TABLE squad (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	match_id INT NOT NULL REFERENCES match(id) ON DELETE CASCADE,
	team_id INT NOT NULL REFERENCES team(id),
	player_id INT NOT NULL REFERENCES player(id),
	UNIQUE(match_id, team_id, player_id)
);

CREATE TABLE how_out (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	type VARCHAR(255) UNIQUE
);
INSERT INTO how_out (type) VALUES ('Caught'), ('Bowled'), ('CaughtAndBowled'), ('LBW'), ('NotOut'), ('RunOut'), ('Stumped'), ('Retired');

CREATE TABLE innings (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	match_id INT NOT NULL REFERENCES match(id) ON DELETE CASCADE,
    team_id INT NOT NULL REFERENCES team(id),
	innings INT NOT NULL,
	extras INT NOT NULL,
	UNIQUE (match_id, team_id, innings)
);
	
CREATE TABLE batting_scorecard (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
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
	CONSTRAINT batting_scorecard_runs_non_negative CHECK (runs >= 0),
	CONSTRAINT batting_scorecard_mins_non_negative CHECK (mins >= 0),
	CONSTRAINT batting_scorecard_balls_non_negative CHECK (balls >= 0),
	CONSTRAINT batting_scorecard_fours_non_negative CHECK (fours >= 0),
	CONSTRAINT batting_scorecard_sixes_non_negative CHECK (sixes >= 0)
);

CREATE TABLE bowling_scorecard (
    id INT NOT NULL IDENTITY (1, 1) PRIMARY KEY,
	innings_id INT NOT NULL REFERENCES innings(id) ON DELETE CASCADE,
	bowler_id INT NOT NULL REFERENCES player(id),
	overs NUMERIC(3, 1) NOT NULL DEFAULT 0,
	maidens INT NOT NULL DEFAULT 0,
	runs_conceded INT NOT NULL DEFAULT 0,
    wickets INT NOT NULL DEFAULT 0,
	CONSTRAINT bowling_scorecard_overs_non_negative CHECK (overs >= 0),
	CONSTRAINT bowling_scorecard_maidens_non_negative CHECK (maidens >= 0),
	CONSTRAINT bowling_scorecard_runs_conceded_non_negative CHECK (runs_conceded >= 0),
	CONSTRAINT bowling_scorecard_wickets_non_negative CHECK (wickets >= 0)
);

CREATE TABLE fall_of_wicket_scorecard (
	innings_id INT NOT NULL REFERENCES innings(id) ON DELETE CASCADE,
	runs INT NOT NULL DEFAULT 0,
	wickets INT NOT NULL DEFAULT 0,
	PRIMARY KEY (innings_id, wickets),
	CONSTRAINT fall_of_wicket_scorecard_runs_non_negative CHECK (runs >= 0),
	CONSTRAINT fall_of_wicket_scorecard_wickets_non_negative CHECK (wickets >= 0)
);