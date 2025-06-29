#!/usr/bin/env python3
"""Ultra-simple voice assistant using existing tools"""
import subprocess
import json
import datetime
import os

# Use existing whisper and ollama
def transcribe_audio():
    """Record and transcribe using existing whisper server"""
    # Record 3 seconds
    subprocess.run(["sox", "-d", "-r", "16000", "-c", "1", "temp.wav", "trim", "0", "3"])
    
    # Send to existing whisper server
    with open("temp.wav", "rb") as f:
        result = subprocess.run([
            "curl", "-s", "-X", "POST",
            "http://localhost:2022/v1/audio/transcriptions",
            "-F", "file=@temp.wav",
            "-F", "model=whisper-1"
        ], capture_output=True, text=True)
    
    try:
        return json.loads(result.stdout)["text"]
    except:
        return ""

def ask_ollama(prompt):
    """Ask ollama phi3:mini"""
    result = subprocess.run([
        "ollama", "run", "phi3:mini", prompt
    ], capture_output=True, text=True)
    return result.stdout.strip()

def log_chat(prompt, response):
    """Simple logging"""
    log_dir = os.path.expanduser("~/ai-logs")
    os.makedirs(log_dir, exist_ok=True)
    
    with open(f"{log_dir}/chat.jsonl", "a") as f:
        f.write(json.dumps({
            "t": datetime.datetime.now().strftime("%H:%M"),
            "p": prompt,
            "r": response[:100]  # Truncate for logs
        }) + "\n")

# Main loop
print("🎤 Voice Assistant Ready (Say something after the beep)")
print("   Press Ctrl+C to stop")

try:
    while True:
        # Beep
        subprocess.run(["say", "beep"])
        
        # Listen
        text = transcribe_audio()
        
        if text:
            print(f"You: {text}")
            
            # Get response
            response = ask_ollama(text)
            print(f"AI: {response}")
            
            # Log it
            log_chat(text, response)
            
            # Speak it
            subprocess.run(["say", response])
        
        # Small pause
        subprocess.run(["sleep", "1"])
        
except KeyboardInterrupt:
    print("\n👋 Goodbye!")
    os.remove("temp.wav")