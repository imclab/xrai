#!/usr/bin/env python3
"""
Desktop overlay like GeekTool - ambient background display
Shows system activity, network, tasks in semi-transparent way
"""
import tkinter as tk
from tkinter import ttk
import subprocess
import json
import os
import threading
import time
from datetime import datetime
import requests

class DesktopOverlay:
    def __init__(self):
        self.root = tk.Tk()
        self.setup_desktop_window()
        self.create_ambient_widgets()
        self.start_background_monitoring()
        
    def setup_desktop_window(self):
        """Configure as desktop background overlay"""
        # Remove all window decorations
        self.root.overrideredirect(True)
        
        # Set to desktop level (behind other windows)
        self.root.wm_attributes('-topmost', False)
        self.root.lower()  # Send to back
        
        # Make semi-transparent
        self.root.wm_attributes('-alpha', 0.3)  # Very subtle
        
        # Set to background color
        self.root.configure(bg='black')
        
        # Position in corner, small size
        self.root.geometry('250x400+50+100')  # Top-left area
        
        # Make it click-through (optional)
        # self.root.wm_attributes('-disabled', True)
        
    def create_ambient_widgets(self):
        """Create ambient info displays"""
        # System status section
        self.system_frame = tk.Frame(self.root, bg='black')
        self.system_frame.pack(fill='x', padx=10, pady=5)
        
        tk.Label(self.system_frame, text="SYSTEM", fg='#00ff00', bg='black', 
                font=('Monaco', 8, 'bold')).pack(anchor='w')
        
        self.cpu_label = tk.Label(self.system_frame, text="CPU: --", 
                                 fg='#666666', bg='black', font=('Monaco', 7))
        self.cpu_label.pack(anchor='w')
        
        self.memory_label = tk.Label(self.system_frame, text="MEM: --", 
                                    fg='#666666', bg='black', font=('Monaco', 7))
        self.memory_label.pack(anchor='w')
        
        self.network_label = tk.Label(self.system_frame, text="NET: --", 
                                     fg='#666666', bg='black', font=('Monaco', 7))
        self.network_label.pack(anchor='w')
        
        # Separator
        tk.Frame(self.root, height=1, bg='#333333').pack(fill='x', pady=5)
        
        # XRAI Activity section
        self.activity_frame = tk.Frame(self.root, bg='black')
        self.activity_frame.pack(fill='x', padx=10, pady=5)
        
        tk.Label(self.activity_frame, text="XRAI ACTIVITY", fg='#00ffff', bg='black',
                font=('Monaco', 8, 'bold')).pack(anchor='w')
        
        # Recent commands (scrolling)
        self.commands_text = tk.Text(self.activity_frame, height=8, width=30,
                                   bg='black', fg='#ffff00', font=('Monaco', 6),
                                   wrap=tk.WORD, borderwidth=0, highlightthickness=0)
        self.commands_text.pack(anchor='w')
        
        # Separator
        tk.Frame(self.root, height=1, bg='#333333').pack(fill='x', pady=5)
        
        # Project structure
        self.project_frame = tk.Frame(self.root, bg='black')
        self.project_frame.pack(fill='x', padx=10, pady=5)
        
        tk.Label(self.project_frame, text="PROJECT TREE", fg='#ff6600', bg='black',
                font=('Monaco', 8, 'bold')).pack(anchor='w')
        
        self.tree_text = tk.Text(self.project_frame, height=6, width=30,
                               bg='black', fg='#999999', font=('Monaco', 6),
                               wrap=tk.NONE, borderwidth=0, highlightthickness=0)
        self.tree_text.pack(anchor='w')
        
        # Network activity (bottom)
        self.net_frame = tk.Frame(self.root, bg='black')
        self.net_frame.pack(fill='x', padx=10, pady=5)
        
        tk.Label(self.net_frame, text="NETWORK", fg='#ff00ff', bg='black',
                font=('Monaco', 8, 'bold')).pack(anchor='w')
        
        self.connections_label = tk.Label(self.net_frame, text="Connections: --",
                                        fg='#666666', bg='black', font=('Monaco', 7))
        self.connections_label.pack(anchor='w')
        
    def update_system_stats(self):
        """Update system statistics"""
        try:
            # CPU usage
            top_result = subprocess.run(['top', '-l', '1', '-n', '0'], 
                                      capture_output=True, text=True, timeout=3)
            cpu_line = [l for l in top_result.stdout.split('\n') if 'CPU usage' in l]
            if cpu_line:
                cpu_text = cpu_line[0].split(':')[1].strip()[:20]
                self.cpu_label.config(text=f"CPU: {cpu_text}")
            
            # Memory usage
            mem_result = subprocess.run(['vm_stat'], capture_output=True, text=True, timeout=3)
            if mem_result.returncode == 0:
                lines = mem_result.stdout.split('\n')
                free_line = [l for l in lines if 'Pages free' in l]
                if free_line:
                    self.memory_label.config(text="MEM: Active")
            
            # Network (simple check)
            ping_result = subprocess.run(['ping', '-c', '1', '8.8.8.8'], 
                                       capture_output=True, text=True, timeout=2)
            net_status = "Online" if ping_result.returncode == 0 else "Offline"
            self.network_label.config(text=f"NET: {net_status}")
            
        except Exception as e:
            print(f"System stats error: {e}")
            
    def update_xrai_activity(self):
        """Update XRAI activity log"""
        try:
            log_file = os.path.expanduser("~/xrai/system_agent.jsonl")
            if os.path.exists(log_file):
                with open(log_file, 'r') as f:
                    lines = f.readlines()[-10:]  # Last 10 events
                
                # Clear and update
                self.commands_text.delete(1.0, tk.END)
                
                for line in reversed(lines):  # Most recent first
                    try:
                        event = json.loads(line)
                        timestamp = event.get('timestamp', '')[:5]  # HH:MM
                        event_text = event.get('event', '')[:40]    # Truncate
                        
                        self.commands_text.insert(tk.END, f"{timestamp} {event_text}\n")
                    except:
                        pass
                        
        except Exception as e:
            self.commands_text.delete(1.0, tk.END)
            self.commands_text.insert(tk.END, f"Log error: {e}\n")
            
    def update_project_tree(self):
        """Update project structure display"""
        try:
            # Simple tree of current work
            tree_display = """Curio/
├── Voice Agent ✓
├── System Control ✓
├── Child Agents ✓
├── Desktop HUD ⚡
└── Integration ⏳

XRAI/
├── Conversations
├── System Logs
└── Agent Status
"""
            self.tree_text.delete(1.0, tk.END)
            self.tree_text.insert(tk.END, tree_display)
            
        except Exception as e:
            print(f"Project tree error: {e}")
            
    def update_network_activity(self):
        """Update network connections"""
        try:
            # Count active connections
            netstat_result = subprocess.run(['netstat', '-an'], 
                                          capture_output=True, text=True, timeout=3)
            if netstat_result.returncode == 0:
                lines = netstat_result.stdout.split('\n')
                established = [l for l in lines if 'ESTABLISHED' in l]
                self.connections_label.config(text=f"Connections: {len(established)}")
            
        except Exception as e:
            self.connections_label.config(text="Connections: Error")
            
    def background_monitor(self):
        """Background monitoring loop"""
        while True:
            try:
                self.update_system_stats()
                self.update_xrai_activity()
                self.update_project_tree()
                self.update_network_activity()
                
                time.sleep(5)  # Update every 5 seconds
                
            except Exception as e:
                print(f"Monitor error: {e}")
                time.sleep(10)
                
    def start_background_monitoring(self):
        """Start background monitoring thread"""
        monitor_thread = threading.Thread(target=self.background_monitor, daemon=True)
        monitor_thread.start()
        
    def run(self):
        """Start the desktop overlay"""
        print("🖥️  Starting Desktop Overlay (GeekTool style)...")
        
        try:
            # Keep window in background
            self.root.after(1000, self.keep_in_background)
            self.root.mainloop()
        except KeyboardInterrupt:
            self.root.quit()
            
    def keep_in_background(self):
        """Ensure window stays in background"""
        self.root.lower()
        self.root.after(5000, self.keep_in_background)  # Check every 5 seconds

if __name__ == "__main__":
    overlay = DesktopOverlay()
    overlay.run()