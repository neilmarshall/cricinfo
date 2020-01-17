#! venv/bin/python3

import argparse
import json
import re
import sys
from shutil import copytree, rmtree

import requests

from resources.data import *

parser = argparse.ArgumentParser(description="Runs HTTP requests to generate CricInfo HTML output")
parser.add_argument('-p', '--port', type=int, default=5001,
                    help="port number on which an instance of Cricinfo.UI is listening (default is %(default)s)")
parser.add_argument('-s', '--stage', type=int, default=5, choices=[0, 1, 2,3, 4, 5],
                    help="stage where requests will halt; options are: "
                         "0 - scorecard page, "
                         "1 - 4 - innings page for each innings, "
                         "5 - verification page (default is %(default)s)")
args = parser.parse_args()

# instantiate Session object to store cookies and session state
session = requests.Session()

try:
    # GET request to scorecard/scorecard to get CSRF token
    response = session.get(f'https://localhost:{args.port}/scorecard/scorecard', verify=False)
    token = re.search("__RequestVerificationToken\\\" type=\\\"hidden\" value=\\\"(?P<token>[^\"]*)\"",
                      response.text).groupdict()['token']

    # load resources and create form parameters
    with open('resources/south_africa-england-26-12-18.json', 'r') as f:
        json = json.loads(f.read())

    scorecard_data = {"__RequestVerificationToken": token,
                      "Venue": json['Venue'],
                      "DateOfFirstDay": json['DateOfFirstDay'],
                      "HomeTeam": json["HomeTeam"],
                      "AwayTeam": json["AwayTeam"],
                      "Result": 0,
                      "HomeSquad": '\n'.join(json["HomeSquad"]),
                      "AwaySquad": '\n'.join(json["AwaySquad"])}

    def innings_data(team, innings, extras, batting_sorecard, bowling_scorecard, fall_of_wicket_scorecard):
        return {"__RequestVerificationToken": token,
                "Team": team,
                "Innings": innings,
                "Extras": extras,
                "BattingScorecard": batting_sorecard,
                "BowlingScorecard": bowling_scorecard,
                "FallOfWicketScorecard": fall_of_wicket_scorecard}

    # generic helper method to parameterise POST requests
    def post_request(session, port, endpoint, data):
        url = f'https://localhost:{port}/scorecard/{endpoint}'
        return session.post(url, verify=False, data=data)

    if args.stage >= 1:
        response = post_request(session, args.port, 'scorecard', scorecard_data)

    if args.stage >= 2:
        response = post_request(session, args.port, 'innings',
                innings_data(json["AwayTeam"], 1, 7,
                    BattingScorecard1, BowlingScorecard1, FallOfWicketScorecard1))

    if args.stage >= 3:
        response = post_request(session, args.port, 'innings',
                innings_data(json["HomeTeam"], 1, 7,
                    BattingScorecard2, BowlingScorecard2, FallOfWicketScorecard2))

    if args.stage >= 4:
        response = post_request(session, args.port, 'innings',
                innings_data(json["AwayTeam"], 2, 22,
                    BattingScorecard3, BowlingScorecard3, FallOfWicketScorecard3))

    if args.stage == 5:
        response = post_request(session, args.port, 'innings',
                innings_data(json["HomeTeam"], 2, 13,
                    BattingScorecard4, BowlingScorecard4, FallOfWicketScorecard4))

    # collect, parse and write output
    output_text = response.text

    if sys.platform == 'win32':
        output_text = re.sub(r'href="/', 'href="', output_text)
        output_text = re.sub(r'src="/', 'src="', output_text)

    # copy static content in 'wwwroot'
    rmtree('output', ignore_errors=True)
    copytree('../Cricinfo.UI/wwwroot', './output')

    with open('output/index.html', 'w') as f:
        f.write(output_text)

    if response.status_code == requests.codes.ok:
        print(f'completed successfully - status code {response.status_code}')

except requests.exceptions.ConnectionError as err:
    print("ERROR :: HTTP Service Unavailable")
