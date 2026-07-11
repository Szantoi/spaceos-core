#!/usr/bin/env python3
"""
Start one or more servers, wait for them to be ready, run a command, then clean up.
"""

import subprocess
import socket
import time
import sys
import argparse

def is_server_ready(port, timeout=30):
    start_time = time.time()
    while time.time() - start_time < timeout:
        try:
            with socket.create_connection(('localhost', port), timeout=1):
                return True
        except (socket.error, ConnectionRefusedError):
            time.sleep(0.5)
    return False

def main():
    parser = argparse.ArgumentParser(description='Run command with one or more servers')
    parser.add_argument('--server', action='append', dest='servers', required=True)
    parser.add_argument('--port', action='append', dest='ports', type=int, required=True)
    parser.add_argument('--timeout', type=int, default=30)
    parser.add_argument('command', nargs=argparse.REMAINDER)

    args = parser.parse_args()
    if args.command and args.command[0] == '--':
        args.command = args.command[1:]

    server_processes = []
    try:
        for i, (cmd, port) in enumerate(zip(args.servers, args.ports)):
            process = subprocess.Popen(cmd, shell=True)
            server_processes.append(process)
            if not is_server_ready(port, timeout=args.timeout):
                raise RuntimeError(f"Server failed on port {port}")

        result = subprocess.run(args.command)
        sys.exit(result.returncode)
    finally:
        for process in server_processes:
            process.terminate()
            process.wait()

if __name__ == '__main__':
    main()
