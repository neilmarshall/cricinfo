#! venv/bin/python3

import argparse
import re
import sys
from shutil import copytree, rmtree

import requests

import resources.south_africa_england_2019_12_26 as s_19_12_26
import resources.south_africa_england_2020_01_03 as s_20_01_03
import resources.south_africa_england_2020_01_16 as s_20_01_16
import resources.south_africa_england_2020_01_24 as s_20_01_24

parser = argparse.ArgumentParser(description="Runs HTTP requests to generate CricInfo HTML output")
parser.add_argument('-p', '--port', type=int, default=5001,
                    help="port number on which an instance of Cricinfo.UI is listening (default is %(default)s)")
parser.add_argument('-s', '--stage', type=int, default=0, choices=[0, 1, 2,3, 4, 5, 6],
                    help="stage where requests will halt; options are: "
                         "0 - scorecard page, "
                         "1 - 4 - innings page for each innings, "
                         "5 - verification page, "
                         "6 - submit verification page (default is %(default)s)")
parser.add_argument('-c', '--scorecard', default="s_19_12_26",
                    choices=["s_19_12_26", "s_20_01_03", "s_20_01_16", "s_20_01_24"],
                    help="Scorecard to process (default is %(default)s)")

args = parser.parse_args()

scorecard = {'s_19_12_26': s_19_12_26, 's_20_01_03': s_20_01_03,
             's_20_01_16': s_20_01_16, 's_20_01_24': s_20_01_24}[args.scorecard]

# instantiate Session object to store cookies and session state
session = requests.Session()

try:
    # GET request to scorecard/scorecard to get CSRF token
    response = session.get(f'https://localhost:{args.port}/scorecard/scorecard', verify=False)
    token = re.search("__RequestVerificationToken\\\" type=\\\"hidden\" value=\\\"(?P<token>[^\"]*)\"",
                      response.text).groupdict()['token']

    # create form parameters
    scorecard_data = {"__RequestVerificationToken": token,
                      "Venue": scorecard.Venue,
                      "DateOfFirstDay": scorecard.DateOfFirstDay,
                      "HomeTeam": scorecard.HomeTeam,
                      "AwayTeam": scorecard.AwayTeam,
                      "Result": scorecard.Result,
                      "HomeSquad": '\n'.join(scorecard.HomeSquad),
                      "AwaySquad": '\n'.join(scorecard.AwaySquad)}

    def innings_data(innings, idx):
        return {"__RequestVerificationToken": token,
                "Innings": innings,
                "Team": scorecard.TeamOrder[idx],
                "Extras": scorecard.Extras[idx],
                "BattingScorecard": scorecard.BattingScorecard[idx],
                "BowlingScorecard": scorecard.BowlingScorecard[idx],
                "FallOfWicketScorecard": scorecard.FallOfWicketScorecard[idx]}

    # generic helper method to parameterise POST requests
    def post_request(endpoint, data):
        url = f'https://localhost:{args.port}/scorecard/{endpoint}'
        return session.post(url, verify=False, data=data)

    if args.stage >= 1:
        response = post_request('scorecard', scorecard_data)

    if args.stage >= 2:
        response = post_request('innings?handler=AddAnotherInnings', innings_data(1, 0))

    if args.stage >= 3:
        response = post_request('innings?handler=AddAnotherInnings', innings_data(1, 1))

    if args.stage >= 4:
        # special case to allow for data with just three innings
        if args.scorecard == 's_20_01_16':
            response = post_request('innings?handler=SubmitAllInnings', innings_data(2, 2))
        else:
            response = post_request('innings?handler=AddAnotherInnings', innings_data(2, 2))

    if args.stage >= 5:
        # special case to allow for data with just three innings
        if args.stage == 5 and args.scorecard == 's_20_01_16':
            raise ValueError('this scorecard does not support a 4th innings')
        elif args.scorecard != 's_20_01_16':
            response = post_request('innings?handler=AddAnotherInnings', innings_data(2, 3))

    if args.stage >= 6:
        response = post_request('verification', {"__RequestVerificationToken": token})

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
