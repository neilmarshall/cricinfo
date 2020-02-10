#! venv/bin/python3

import os
import datetime
import subprocess
import sys

BASE_DIR = "backups"

if not os.path.exists(BASE_DIR):
    os.mkdir(BASE_DIR)

filename = os.path.join(os.getcwd(), BASE_DIR, f"{datetime.datetime.now().strftime('%Y-%m-%dT%H-%M-%S')}.sql")

if sys.platform == 'win32':
    subprocess.run(["pg_dump", "-d", "cricinfo", "-f", filename])
elif sys.platform == 'darwin':
    subprocess.run(["/Library/PostgreSQL/11/bin/pg_dump", "-d", "cricinfo", "-f", filename])
else:
    raise OSError("OS not supported")
