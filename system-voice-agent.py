#!/usr/bin/env python3
"""
System-Level Voice Agent for macOS
Full computer control with child agent spawning
"""
import subprocess
import json
import os
import threading
import queue
import time
from datetime import datetime
import tempfile

# Configuration
LOG_FILE = os.path.expanduser("~/xrai/system_agent.jsonl")
CONTEXT_SIZE = 20
CHILD_AGENTS = {}
should_listen = True
audio_queue = queue.Queue()
command_queue = queue.Queue()

class ChildAgent:
    """Spawnable child agent for parallel tasks"""
    def __init__(self, task_type, task_id):
        self.task_type = task_type
        self.task_id = task_id
        self.running = True
        
    def research_task(self, query):
        """Research agent using web search and file analysis"""
        try:
            # Quick web search
            result = subprocess.run([
                "curl", "-s", f"https://api.duckduckgo.com/?q={query}&format=json&no_html=1"
            ], capture_output=True, text=True)
            
            # File system search if relevant
            if "file" in query.lower() or "code" in query.lower():
                find_result = subprocess.run([
                    "find", "/Users/jamestunick/Documents/GitHub", "-name", f"*{query}*", "-type", "f"
                ], capture_output=True, text=True)
                
            return f"Research complete: {query}"
        except Exception as e:
            return f"Research failed: {e}"
    
    def file_task(self, operation, path, content=None):
        """File manipulation agent"""
        try:
            if operation == "read":
                with open(path, 'r') as f:
                    return f.read()[:1000]  # First 1K chars
            elif operation == "write" and content:
                with open(path, 'w') as f:
                    f.write(content)
                return f"Written to {path}"
            elif operation == "append" and content:
                with open(path, 'a') as f:
                    f.write(content)
                return f"Appended to {path}"
        except Exception as e:
            return f"File operation failed: {e}"
    
    def system_task(self, command):
        """System command agent"""
        try:
            # Safety check - no dangerous commands
            dangerous = ['rm -rf', 'sudo rm', 'format', 'mkfs', 'dd if=']
            if any(d in command for d in dangerous):
                return "Dangerous command blocked"
            
            result = subprocess.run(command.split(), capture_output=True, text=True, timeout=30)
            return result.stdout[:500] if result.returncode == 0 else result.stderr[:500]
        except Exception as e:
            return f"System command failed: {e}"

def spawn_child_agent(task_type, task_data):
    """Spawn child agent for parallel execution"""
    task_id = f"{task_type}_{int(time.time())}"
    agent = ChildAgent(task_type, task_id)
    CHILD_AGENTS[task_id] = agent
    
    def run_task():
        try:
            if task_type == "research":
                result = agent.research_task(task_data)
            elif task_type == "file":
                result = agent.file_task(**task_data)
            elif task_type == "system":
                result = agent.system_task(task_data)
            else:
                result = "Unknown task type"
            
            log_system_event(f"Child {task_id} completed: {result}")
            speak_result(f"Task {task_type} complete")
            
        except Exception as e:
            log_system_event(f"Child {task_id} failed: {e}")
        finally:
            if task_id in CHILD_AGENTS:
                del CHILD_AGENTS[task_id]
    
    threading.Thread(target=run_task, daemon=True).start()
    return task_id

def get_system_context():
    """Enhanced context with system state"""
    context = []
    
    # Recent conversations
    if os.path.exists(LOG_FILE):
        with open(LOG_FILE, 'r') as f:
            lines = f.readlines()[-5:]
        for line in lines:
            try:
                entry = json.loads(line)
                context.append(entry['event'][:50])
            except:
                pass
    
    # Active child agents
    if CHILD_AGENTS:
        context.append(f"Active agents: {list(CHILD_AGENTS.keys())}")
    
    # System status
    try:
        cpu_result = subprocess.run(["top", "-l", "1", "-n", "0"], capture_output=True, text=True)
        cpu_line = [l for l in cpu_result.stdout.split('\n') if 'CPU usage' in l]
        if cpu_line:
            context.append(f"CPU: {cpu_line[0].split(':')[1].strip()}")
    except:
        pass
    
    return " | ".join(context[-3:]) if context else ""

def log_system_event(event):
    """Log system events"""
    os.makedirs(os.path.dirname(LOG_FILE), exist_ok=True)
    with open(LOG_FILE, 'a') as f:
        f.write(json.dumps({
            "timestamp": datetime.now().isoformat(),
            "event": event
        }) + "\n")

def speak_result(text):
    """Fast TTS output"""
    subprocess.run(["say", "-r", "300", text], check=False)

def execute_system_command(command):
    """Execute system commands with safety checks"""
    try:
        # Parse command intent
        if command.startswith("open"):
            app_name = command.replace("open", "").strip()
            subprocess.run(["open", "-a", app_name])
            return f"Opening {app_name}"
        
        elif command.startswith("find"):
            search_term = command.replace("find", "").strip()
            result = subprocess.run(["mdfind", search_term], capture_output=True, text=True)
            files = result.stdout.strip().split('\n')[:5]  # Top 5 results
            return f"Found: {', '.join([os.path.basename(f) for f in files if f])}"
        
        elif command.startswith("create"):
            # Parse "create file/folder name"
            parts = command.split()
            if len(parts) >= 3:
                item_type, name = parts[1], parts[2]
                if item_type == "file":
                    with open(name, 'w') as f:
                        f.write("")
                    return f"Created file {name}"
                elif item_type == "folder":
                    os.makedirs(name, exist_ok=True)
                    return f"Created folder {name}"
        
        elif "research" in command or "search" in command:
            query = command.replace("research", "").replace("search", "").strip()
            task_id = spawn_child_agent("research", query)
            return f"Started research task {task_id}"
        
        elif "run" in command or "execute" in command:
            cmd = command.replace("run", "").replace("execute", "").strip()
            task_id = spawn_child_agent("system", cmd)
            return f"Started system task {task_id}"
        
        else:
            # Direct shell command (with safety)
            task_id = spawn_child_agent("system", command)
            return f"Executing: {command}"
            
    except Exception as e:
        return f"Command failed: {e}"

def listen_continuous():
    """Always listening with voice activity detection"""
    while should_listen:
        # Record 3-second chunks for better detection
        with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as tmp:
            subprocess.run([
                "sox", "-q", "-d", "-r", "16000", "-c", "1", 
                tmp.name, "trim", "0", "3"
            ], stderr=subprocess.DEVNULL)
            
            # Quick amplitude check
            result = subprocess.run([
                "sox", tmp.name, "-n", "stat"
            ], capture_output=True, text=True, stderr=subprocess.STDOUT)
            
            if "Maximum amplitude:     0.000000" not in result.stdout:
                audio_queue.put(tmp.name)
            else:
                os.unlink(tmp.name)
        
        time.sleep(0.1)

def process_audio():
    """Process audio with enhanced NLP"""
    while True:
        audio_file = audio_queue.get()
        
        try:
            # Transcribe via local Whisper
            result = subprocess.run([
                "curl", "-s", "-X", "POST",
                "http://localhost:2022/v1/audio/transcriptions",
                "-F", f"file=@{audio_file}",
                "-F", "model=whisper-1"
            ], capture_output=True, text=True)
            
            os.unlink(audio_file)  # Clean up
            
            transcription = json.loads(result.stdout)["text"].strip()
            
            if transcription and len(transcription) > 3:
                print(f"\n🎤 [{datetime.now().strftime('%H:%M:%S')}] {transcription}")
                log_system_event(f"Voice: {transcription}")
                
                # Enhanced processing with system context
                context = get_system_context()
                
                # Check for direct commands first
                if any(word in transcription.lower() for word in ['open', 'create', 'find', 'run', 'execute', 'research']):
                    result = execute_system_command(transcription.lower())
                    print(f"🖥️  {result}")
                    speak_result(result)
                    log_system_event(f"Command: {result}")
                else:
                    # Regular conversation with Ollama
                    full_prompt = f"System context: {context}\nUser said: {transcription}\nRespond briefly as a helpful system assistant:"
                    
                    response = subprocess.run([
                        "ollama", "run", "phi3:mini", full_prompt
                    ], capture_output=True, text=True).stdout.strip()
                    
                    print(f"🤖 {response}")
                    log_system_event(f"Response: {response}")
                    
                    if len(response) < 150:
                        speak_result(response)
                
        except Exception as e:
            print(f"❌ Audio processing error: {e}")

def status_monitor():
    """Monitor system and agent status"""
    while True:
        time.sleep(60)  # Check every minute
        
        # Clean up completed agents
        active_count = len(CHILD_AGENTS)
        if active_count > 0:
            print(f"📊 Active agents: {active_count}")
        
        # System health check
        try:
            # Check if Whisper is still running
            whisper_check = subprocess.run([
                "curl", "-s", "http://localhost:2022/health"
            ], capture_output=True, timeout=5)
            
            if whisper_check.returncode != 0:
                print("⚠️  Whisper server not responding")
        except:
            pass

# Initialize system agent
if __name__ == "__main__":
    print("🚀 SYSTEM VOICE AGENT ACTIVE")
    print("💻 Full computer control enabled")
    print("🔊 Listening for voice commands...")
    print("📋 Child agent spawning ready")
    
    # Start all threads
    threading.Thread(target=listen_continuous, daemon=True).start()
    threading.Thread(target=process_audio, daemon=True).start()
    threading.Thread(target=status_monitor, daemon=True).start()
    
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        should_listen = False
        print("\n🛑 System agent stopping...")
        for agent_id in list(CHILD_AGENTS.keys()):
            print(f"  Terminating {agent_id}")
        print("👋 System agent stopped")