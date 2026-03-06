#!/usr/bin/env python3
# 1-click reset pose
"""
Quest 3 Controller Tracker — UDP Receiver
==========================================
Listens for JSON packets from the Unity app and prints them in real-time.

Usage:
    python receiver.py                     # listen on 0.0.0.0:7000
    python receiver.py --port 8000         # custom port
    python receiver.py --save log.jsonl    # also save to a JSONL file

The script prints every packet as pretty-printed JSON with a local timestamp.
Press Ctrl+C to stop.
"""

import argparse
import json
import socket
import sys
from datetime import datetime


def colour(text: str, code: int) -> str:
    """ANSI colour wrapper (degrades gracefully on Windows)."""
    return f"\033[{code}m{text}\033[0m"


def print_snapshot(raw: bytes, seq: int, save_file=None):
    """Decode, pretty-print, and optionally save one JSON snapshot."""
    try:
        data = json.loads(raw.decode("utf-8"))
    except (json.JSONDecodeError, UnicodeDecodeError) as exc:
        print(colour(f"[#{seq}] ⚠ Bad packet: {exc}", 33))
        return

    now = datetime.now().strftime("%H:%M:%S.%f")[:-3]

    # ── Header ──────────────────────────────────────────────────────
    ts = data.get("timestamp", "?")
    ut = data.get("unityTime", 0)
    print(colour(f"\n{'='*72}", 36))
    print(colour(f"  Packet #{seq}   local={now}   unity_t={ut:.3f}   ts={ts}", 36))
    print(colour(f"{'='*72}", 36))

    # ── Per-hand summary ────────────────────────────────────────────
    for hand_key in ("left", "right"):
        h = data.get(hand_key, {})
        valid = h.get("isValid", False)
        label = hand_key.upper().ljust(6)

        if not valid:
            print(f"  {label}  (not connected)")
            continue

        pos = f"pos=({h.get('posX',0):+.3f}, {h.get('posY',0):+.3f}, {h.get('posZ',0):+.3f})"
        rot = (f"rot=({h.get('rotX',0):+.4f}, {h.get('rotY',0):+.4f}, "
               f"{h.get('rotZ',0):+.4f}, {h.get('rotW',0):+.4f})")

        # Collect pressed buttons into a list
        buttons = []
        if h.get("triggerButton"):  buttons.append(f"Trigger({h.get('triggerValue',0):.2f})")
        if h.get("gripButton"):     buttons.append(f"Grip({h.get('gripValue',0):.2f})")
        if h.get("primaryButton"):  buttons.append("Primary(X/A)")
        if h.get("secondaryButton"):buttons.append("Secondary(Y/B)")
        if h.get("thumbstickClick"):buttons.append("ThumbClick")
        if h.get("menuButton"):     buttons.append("Menu")

        # Thumbstick
        tx = h.get("thumbstickX", 0)
        ty = h.get("thumbstickY", 0)
        stick = f"stick=({tx:+.2f},{ty:+.2f})"

        btn_str = ", ".join(buttons) if buttons else "(none)"

        print(f"  {label}  {pos}")
        print(f"          {rot}")
        print(f"          {stick}   buttons: {colour(btn_str, 33 if buttons else 90)}")

    # ── Optional save ───────────────────────────────────────────────
    if save_file is not None:
        data["_received_at"] = now
        data["_seq"] = seq
        save_file.write(json.dumps(data) + "\n")
        save_file.flush()


def main():
    parser = argparse.ArgumentParser(description="Quest 3 Controller Tracker Receiver")
    parser.add_argument("--port", type=int, default=7000,
                        help="UDP port to listen on (default: 7000)")
    parser.add_argument("--host", type=str, default="0.0.0.0",
                        help="Bind address (default: 0.0.0.0)")
    parser.add_argument("--save", type=str, default=None,
                        help="Path to save incoming data as JSONL")
    args = parser.parse_args()

    # Open optional save file
    save_file = open(args.save, "a", encoding="utf-8") if args.save else None

    # Set up UDP socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind((args.host, args.port))

    print(colour(f"🎮  Quest 3 Controller Receiver", 32))
    print(colour(f"   Listening on {args.host}:{args.port}", 32))
    if save_file:
        print(colour(f"   Saving to {args.save}", 32))
    print(colour(f"   Press Ctrl+C to stop.\n", 90))

    seq = 0
    try:
        while True:
            raw, addr = sock.recvfrom(65535)
            seq += 1
            print_snapshot(raw, seq, save_file)
    except KeyboardInterrupt:
        print(colour(f"\n✋  Stopped after {seq} packets.", 31))
    finally:
        sock.close()
        if save_file:
            save_file.close()


if __name__ == "__main__":
    main()
