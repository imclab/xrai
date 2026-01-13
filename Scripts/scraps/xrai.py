#!/usr/bin/env python3
"""XRAI - Minimal always-on voice assistant"""
import subprocess
import json
import os
from datetime import datetime
import threading
import queue
import time

# Paths
LOG_FILE = os.path.expanduser("~/xrai/conversations.jsonl")
CONTEXT_SIZE = 10

# Queues
audio_queue = queue.Queue()
should_listen = True

def get_context():
    """Load last N conversations"""
    if not os.path.exists(LOG_FILE):
        return ""
    
    with open(LOG_FILE, 'r') as f:
        lines = f.readlines()[-CONTEXT_SIZE:]
    
    context = []
    for line in lines:
        try:
            entry = json.loads(line)
            context.append(f"{entry['p']}")
        except:
            pass
    
    return "Recent: " + " | ".join(context[-3:]) if context else ""

def log_convo(prompt, response):
    """Minimal logging"""
    os.makedirs(os.path.dirname(LOG_FILE), exist_ok=True)
    with open(LOG_FILE, 'a') as f:
        f.write(json.dumps({
            "t": datetime.now().strftime("%H:%M"),
            "p": prompt,
            "r": response[:100]
        }) + "\n")

def listen_continuous():
    """Always listening thread"""
    while should_listen:
        # Quick 2-second recordings
        subprocess.run(["sox", "-q", "-d", "-r", "16000", "-c", "1", "temp.wav", "trim", "0", "2"], 
                      stderr=subprocess.DEVNULL)
        
        # Check if there's speech
        result = subprocess.run(["sox", "temp.wav", "-n", "stat"], 
                              capture_output=True, text=True, stderr=subprocess.STDOUT)
        
        if "Maximum amplitude:     0.000000" not in result.stdout:
            audio_queue.put("temp.wav")
        
        time.sleep(0.1)

def process_audio():
    """Process audio with Ollama"""
    while True:
        audio_file = audio_queue.get()
        
        # Transcribe
        result = subprocess.run([
            "curl", "-s", "-X", "POST",
            "http://localhost:2022/v1/audio/transcriptions",
            "-F", f"file=@{audio_file}",
            "-F", "model=whisper-1"
        ], capture_output=True, text=True)
        
        try:
            text = json.loads(result.stdout)["text"].strip()
            if text and len(text) > 3:  # Ignore short noise
                print(f"\n[{datetime.now().strftime('%H:%M')}] 🎤 {text}")
                
                # Get response with context
                context = get_context()
                prompt = f"{context}\nUser: {text}\nAssistant:" if context else text
                
                response = subprocess.run(
                    ["ollama", "run", "phi3:mini", prompt],
                    capture_output=True, text=True
                ).stdout.strip()
                
                print(f"[{datetime.now().strftime('%H:%M')}] 🤖 {response}")
                log_convo(text, response)
                
                # Optional TTS
                if len(response) < 100:
                    subprocess.run(["say", "-r", "250", response])
        except:
            pass

# Start threads
print("🎧 XRAI Active (Ctrl+C to stop)")
threading.Thread(target=listen_continuous, daemon=True).start()
threading.Thread(target=process_audio, daemon=True).start()

try:
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    should_listen = False
    print("\n👋 XRAI stopped")