#!/usr/bin/env python3
"""XRAI HUD - Minimal heads-up display"""
import tkinter as tk
from tkinter import font
import json
import os
from datetime import datetime
from collections import deque

class XRAIHUD:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("XRAI")
        
        # Transparent, always on top, click-through
        self.root.attributes('-alpha', 0.8)
        self.root.attributes('-topmost', True)
        self.root.overrideredirect(True)
        
        # Position: top-right corner
        self.root.geometry("300x150+{}+20".format(
            self.root.winfo_screenwidth() - 320
        ))
        
        # Dark theme
        self.root.configure(bg='black')
        
        # Data
        self.history = deque(maxlen=20)
        self.log_file = os.path.expanduser("~/xrai/conversations.jsonl")
        
        # UI Elements
        self.setup_ui()
        
        # Update loop
        self.update_display()
        
    def setup_ui(self):
        # Status line
        self.status = tk.Label(
            self.root, 
            text="XRAI ▶", 
            fg='#00ff00', 
            bg='black',
            font=('Monaco', 10)
        )
        self.status.pack(anchor='nw', padx=5, pady=2)
        
        # Last interaction
        self.last_text = tk.Label(
            self.root,
            text="...",
            fg='#888888',
            bg='black',
            font=('Monaco', 8),
            wraplength=280,
            justify='left'
        )
        self.last_text.pack(anchor='nw', padx=5)
        
        # Activity sparkline (text-based for now)
        self.sparkline = tk.Label(
            self.root,
            text="▁▂▃▄▅▆▇█",
            fg='#00ffff',
            bg='black',
            font=('Monaco', 12)
        )
        self.sparkline.pack(anchor='nw', padx=5, pady=5)
        
        # Stats
        self.stats = tk.Label(
            self.root,
            text="0 msg | 0.0s | CPU:1%",
            fg='#666666',
            bg='black',
            font=('Monaco', 8)
        )
        self.stats.pack(anchor='nw', padx=5)
        
    def update_display(self):
        try:
            # Read latest from log
            if os.path.exists(self.log_file):
                with open(self.log_file, 'r') as f:
                    lines = f.readlines()
                    if lines:
                        latest = json.loads(lines[-1])
                        
                        # Update last text
                        self.last_text.config(
                            text=f"{latest['t']} {latest['p'][:50]}..."
                        )
                        
                        # Activity indicator
                        activity = len(lines) % 8
                        spark = "▁▂▃▄▅▆▇█"
                        self.sparkline.config(
                            text=spark[activity:] + spark[:activity]
                        )
                        
                        # Stats
                        self.stats.config(
                            text=f"{len(lines)} msg | {datetime.now().strftime('%H:%M')}"
                        )
                        
                        # Status color
                        if len(lines) > len(self.history):
                            self.status.config(fg='#00ff00')  # Active
                        else:
                            self.status.config(fg='#666666')  # Idle
                            
                        self.history = deque(lines, maxlen=20)
                        
        except:
            pass
        
        # Refresh every 500ms
        self.root.after(500, self.update_display)
    
    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    hud = XRAIHUD()
    hud.run()