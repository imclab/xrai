#!/usr/bin/env python3
"""
Semi-transparent system overlay HUD for macOS
Lives as floating window, not browser
"""
import tkinter as tk
from tkinter import ttk
import json
import os
import threading
import time
from datetime import datetime

class SystemOverlayHUD:
    def __init__(self):
        self.root = tk.Tk()
        self.setup_window()
        self.create_widgets()
        self.start_monitoring()
        
    def setup_window(self):
        """Configure transparent overlay window"""
        # Remove window decorations
        self.root.overrideredirect(True)
        
        # Set window properties
        self.root.wm_attributes('-topmost', True)  # Always on top
        self.root.wm_attributes('-alpha', 0.8)     # Semi-transparent
        self.root.configure(bg='black')
        
        # Position in top-right corner
        self.root.geometry('300x150+1620+50')  # Adjust for your screen
        
        # Make window stay on screen
        self.root.lift()
        
    def create_widgets(self):
        """Create minimal HUD elements"""
        # Status indicator
        self.status_label = tk.Label(
            self.root, 
            text="🎯 XRAI", 
            fg='#00ff00', 
            bg='black',
            font=('Monaco', 12, 'bold')
        )
        self.status_label.pack(pady=2)
        
        # Agent count
        self.agents_label = tk.Label(
            self.root,
            text="Agents: 0",
            fg='#00ffff',
            bg='black',
            font=('Monaco', 9)
        )
        self.agents_label.pack()
        
        # Recent activity (mini timeline)
        self.activity_frame = tk.Frame(self.root, bg='black')
        self.activity_frame.pack(fill='x', padx=5, pady=2)
        
        # Activity dots (last 10 actions)
        self.activity_dots = []
        for i in range(10):
            dot = tk.Label(
                self.activity_frame,
                text="●",
                fg='#333333',
                bg='black',
                font=('Monaco', 8)
            )
            dot.pack(side='left', padx=1)
            self.activity_dots.append(dot)
        
        # Time
        self.time_label = tk.Label(
            self.root,
            text="",
            fg='#666666',
            bg='black',
            font=('Monaco', 8)
        )
        self.time_label.pack(pady=2)
        
        # Mini project structure (3 lines max)
        self.project_label = tk.Label(
            self.root,
            text="📂 Curio → Voice → HUD",
            fg='#ffff00',
            bg='black',
            font=('Monaco', 8)
        )
        self.project_label.pack()
        
        # Add drag functionality
        self.root.bind('<Button-1>', self.start_drag)
        self.root.bind('<B1-Motion>', self.drag_window)
        
        # Double-click to minimize/restore
        self.root.bind('<Double-Button-1>', self.toggle_size)
        self.minimized = False
        
    def start_drag(self, event):
        """Start dragging window"""
        self.x = event.x
        self.y = event.y
        
    def drag_window(self, event):
        """Drag window around screen"""
        deltax = event.x - self.x
        deltay = event.y - self.y
        x = self.root.winfo_x() + deltax
        y = self.root.winfo_y() + deltay
        self.root.geometry(f"+{x}+{y}")
        
    def toggle_size(self, event):
        """Toggle between full and minimal size"""
        if self.minimized:
            self.root.geometry('300x150')
            self.minimized = False
        else:
            self.root.geometry('100x30')
            self.minimized = True
            
    def update_agent_count(self):
        """Update active agent count"""
        try:
            # Count running python processes with 'xrai' in name
            import subprocess
            result = subprocess.run(['pgrep', '-f', 'xrai'], capture_output=True, text=True)
            count = len(result.stdout.strip().split('\n')) if result.stdout.strip() else 0
            self.agents_label.config(text=f"Agents: {count}")
        except:
            self.agents_label.config(text="Agents: ?")
            
    def update_activity(self):
        """Update activity timeline dots"""
        log_file = os.path.expanduser("~/xrai/system_agent.jsonl")
        if not os.path.exists(log_file):
            return
            
        try:
            with open(log_file, 'r') as f:
                lines = f.readlines()[-10:]  # Last 10 events
                
            # Reset all dots
            for dot in self.activity_dots:
                dot.config(fg='#333333')
                
            # Color dots based on recent activity
            for i, line in enumerate(lines):
                if i < len(self.activity_dots):
                    try:
                        event = json.loads(line)
                        # Color code by event type
                        if 'Voice:' in event.get('event', ''):
                            color = '#00ff00'  # Green for voice
                        elif 'Command:' in event.get('event', ''):
                            color = '#ff6600'  # Orange for commands
                        elif 'Child' in event.get('event', ''):
                            color = '#00ffff'  # Cyan for child agents
                        else:
                            color = '#666666'  # Gray for other
                            
                        self.activity_dots[i].config(fg=color)
                    except:
                        pass
        except:
            pass
            
    def update_time(self):
        """Update time display"""
        current_time = datetime.now().strftime("%H:%M:%S")
        self.time_label.config(text=current_time)
        
    def update_display(self):
        """Update all display elements"""
        while True:
            try:
                self.update_agent_count()
                self.update_activity()
                self.update_time()
                time.sleep(2)  # Update every 2 seconds
            except Exception as e:
                print(f"HUD update error: {e}")
                time.sleep(5)
                
    def start_monitoring(self):
        """Start background monitoring thread"""
        monitor_thread = threading.Thread(target=self.update_display, daemon=True)
        monitor_thread.start()
        
    def run(self):
        """Start the HUD"""
        try:
            self.root.mainloop()
        except KeyboardInterrupt:
            self.root.quit()

if __name__ == "__main__":
    print("🎯 Starting System Overlay HUD...")
    hud = SystemOverlayHUD()
    hud.run()