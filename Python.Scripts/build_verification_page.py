#! venv/bin/python3

import argparse
import json
import re
import sys

import requests

from resources.data import BattingScorecard, BowlingScorecard, FallOfWicketScorecard

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
                      "HomeSquad": '\n'.join(json["HomeSquad"][:2]),
                      "AwaySquad": '\n'.join(json["AwaySquad"][:2])}

    innings_data = {"__RequestVerificationToken": token,
                    "Team": json["HomeTeam"],
                    "Innings": 1,
                    "Extras": json["Scores"][0]["Extras"],
                    "BattingScorecard": BattingScorecard,
                    "BowlingScorecard": BowlingScorecard,
                    "FallOfWicketScorecard": FallOfWicketScorecard}

    # generic helper method to parameterise POST requests
    def post_request(session, port, endpoint, data):
        url = f'https://localhost:{port}/scorecard/{endpoint}'
        return session.post(url, verify=False, data=data)

    if args.stage >= 1:
        response = post_request(session, args.port, 'scorecard', scorecard_data)

    if args.stage >= 2:
        response = post_request(session, args.port, 'innings', innings_data)

    if args.stage >= 3:
        response = post_request(session, args.port, 'innings', innings_data)

    if args.stage >= 4:
        response = post_request(session, args.port, 'innings', innings_data)

    if args.stage == 5:
        response = post_request(session, args.port, 'innings', innings_data)

    # collect, parse and write output
    output_text = response.text

    if sys.platform == 'win32':
        output_text = re.sub(r'href="/', 'href="', output_text)
        output_text = re.sub(r'src="/', 'src="', output_text)

    with open('output/index.html', 'w') as f:
        f.write(output_text)

    if response.status_code == requests.codes.ok:
        print(f'completed successfully - status code {response.status_code}')
except requests.exceptions.ConnectionError as err:
    print("ERROR :: HTTP Service Unavailable")
