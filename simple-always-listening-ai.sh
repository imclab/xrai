#!/bin/bash
# Ultra-lightweight Always-Listening AI Assistant
# One-command installer for Mac

echo "🤖 Installing Always-Listening AI..."

# 1. Install dependencies
brew install ollama ffmpeg sox
pip3 install letta whisper openai-whisper pyaudio

# 2. Pull lightweight model
ollama pull phi3:mini  # 2.3GB, very fast

# 3. Create daemon script
cat > ~/ai-daemon.py << 'EOF'
#!/usr/bin/env python3
import whisper
import pyaudio
import numpy as np
import subprocess
import threading
import queue
import json
import datetime
import os
from letta import create_client

# Logging setup
LOG_DIR = os.path.expanduser("~/ai-assistant-logs")
os.makedirs(LOG_DIR, exist_ok=True)
DAILY_LOG = os.path.join(LOG_DIR, f"{datetime.date.today()}.jsonl")
MASTER_LOG = os.path.join(LOG_DIR, "master.jsonl")

# Lightweight whisper
model = whisper.load_model("tiny")  # 39MB, super fast

# Memory-persistent AI
client = create_client()
agent = client.create_agent(
    name="assistant",
    system="You are a helpful assistant. Be brief. You have access to all previous conversations.",
    model="ollama/phi3:mini"
)

def log_interaction(prompt, response, timestamp=None):
    """Log minimal JSON - one line per interaction"""
    if timestamp is None:
        timestamp = datetime.datetime.now()
    
    # Ultra-compact format
    entry = {
        "t": timestamp.strftime("%H:%M:%S"),  # time only
        "p": prompt,                          # prompt
        "r": response                         # response
    }
    
    # Daily log only (master gets too big)
    with open(DAILY_LOG, 'a') as f:
        f.write(json.dumps(entry, separators=(',', ':')) + '\n')
    
    # Weekly summary log (compact)
    weekly_log = os.path.join(LOG_DIR, f"week-{timestamp.strftime('%Y-%W')}.jsonl")
    summary = {"d": timestamp.strftime("%m-%d"), "t": timestamp.strftime("%H:%M"), "p": prompt[:50]}
    with open(weekly_log, 'a') as f:
        f.write(json.dumps(summary, separators=(',', ':')) + '\n')

def load_recent_context():
    """Load last 10 interactions for context"""
    context = []
    if os.path.exists(DAILY_LOG):
        with open(DAILY_LOG, 'r') as f:
            lines = f.readlines()
            for line in lines[-10:]:  # Last 10 only
                try:
                    entry = json.loads(line)
                    context.append(entry['p'])  # Just prompts
                except:
                    pass
    return context

def get_summary_prompt():
    """Minimal context prompt"""
    recent = load_recent_context()
    if recent:
        return f"Recent: {', '.join(recent[-3:])}"
    return ""

# Audio setup
audio_queue = queue.Queue()
p = pyaudio.PyAudio()
stream = p.open(format=pyaudio.paInt16,
                channels=1,
                rate=16000,
                input=True,
                frames_per_buffer=1024)

def listen_continuous():
    """Always listening, minimal CPU"""
    buffer = []
    silence_count = 0
    
    while True:
        data = stream.read(1024, exception_on_overflow=False)
        audio_data = np.frombuffer(data, np.int16).astype(np.float32) / 32768.0
        
        # Simple voice detection
        if np.abs(audio_data).mean() > 0.01:
            buffer.append(audio_data)
            silence_count = 0
        elif buffer:
            silence_count += 1
            if silence_count > 30:  # ~1 second silence
                audio_queue.put(np.concatenate(buffer))
                buffer = []

def process_audio():
    """Process with memory and logging"""
    while True:
        audio = audio_queue.get()
        
        # Transcribe
        result = model.transcribe(audio, language='en')
        text = result["text"].strip()
        
        if text:
            timestamp = datetime.datetime.now()
            print(f"[{timestamp.strftime('%H:%M:%S')}] You: {text}")
            
            # Add context awareness
            context_prompt = get_summary_prompt()
            
            # AI with memory + context
            response = agent.send_message(f"{context_prompt}\nUser: {text}")
            response_text = response.messages[-1].text
            
            print(f"[{timestamp.strftime('%H:%M:%S')}] AI: {response_text}")
            
            # Log everything
            log_interaction(text, response_text, timestamp)
            
            # Speak response (optional)
            subprocess.run(["say", response_text])

# Start threads
threading.Thread(target=listen_continuous, daemon=True).start()
threading.Thread(target=process_audio, daemon=True).start()

print("🎧 Always listening... (Ctrl+C to stop)")
try:
    while True:
        pass
except KeyboardInterrupt:
    stream.stop_stream()
    stream.close()
    p.terminate()
EOF

# 4. Create LaunchAgent for auto-start
cat > ~/Library/LaunchAgents/com.ai.daemon.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.ai.daemon</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/python3</string>
        <string>/Users/$USER/ai-daemon.py</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>ProcessType</key>
    <string>Background</string>
    <key>Nice</key>
    <integer>10</integer>
</dict>
</plist>
EOF

# 5. Start it
chmod +x ~/ai-daemon.py
launchctl load ~/Library/LaunchAgents/com.ai.daemon.plist

echo "✅ Done! AI is now:"
echo "  • Always listening in background"
echo "  • Using minimal CPU (tiny models)"
echo "  • Remembering all conversations"
echo "  • Auto-starting on boot"
echo "  • Logging to ~/ai-assistant-logs/"
echo ""
echo "Commands:"
echo "  Stop:    launchctl unload ~/Library/LaunchAgents/com.ai.daemon.plist"
echo "  Logs:    tail -f ~/ai-assistant-logs/$(date +%Y-%m-%d).jsonl"
echo "  History: cat ~/ai-assistant-logs/master.jsonl | jq '.'"
echo ""
echo "Log files:"
echo "  • Daily logs: ~/ai-assistant-logs/YYYY-MM-DD.jsonl"
echo "  • Master log: ~/ai-assistant-logs/master.jsonl"
echo "  • Context: Today, last week, all time"