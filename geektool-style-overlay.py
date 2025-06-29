#!/usr/bin/env python3
"""
GeekTool-style overlay - Native implementation
Mimics GeekTool's simple, efficient desktop widgets
"""
import tkinter as tk
from tkinter import font
import subprocess
import json
import os
import threading
import time
from datetime import datetime
import requests
from PIL import Image, ImageTk

class GeekletWidget:
    """Base class for GeekTool-style widgets"""
    def __init__(self, root, x, y, width, height, opacity=0.8):
        self.root = root
        self.frame = tk.Toplevel(root)
        self.setup_window(x, y, width, height, opacity)
        
    def setup_window(self, x, y, width, height, opacity):
        """Configure window like GeekTool geeklets"""
        self.frame.overrideredirect(True)  # No decorations
        self.frame.wm_attributes('-topmost', False)  # Behind other windows
        self.frame.wm_attributes('-alpha', opacity)  # Semi-transparent
        self.frame.configure(bg='black')
        self.frame.geometry(f"{width}x{height}+{x}+{y}")
        
        # Send to desktop level
        self.frame.lower()

class ShellWidget(GeekletWidget):
    """Shell command widget (like activity monitor geeklet)"""
    def __init__(self, root, x, y, width, height, command, refresh_interval=10, opacity=0.3):
        super().__init__(root, x, y, width, height, opacity)
        self.command = command
        self.refresh_interval = refresh_interval
        
        # Text widget for output
        self.text = tk.Text(
            self.frame,
            bg='black',
            fg='white',
            font=('HelveticaNeue-Light', 10),
            wrap=tk.WORD,
            borderwidth=0,
            highlightthickness=0,
            selectbackground='#333333'
        )
        self.text.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        
        # Start updating
        self.update_content()
        
    def update_content(self):
        """Execute command and update display"""
        try:
            result = subprocess.run(
                self.command.split(),
                capture_output=True,
                text=True,
                timeout=5
            )
            
            # Clear and update
            self.text.delete(1.0, tk.END)
            self.text.insert(tk.END, result.stdout)
            
        except Exception as e:
            self.text.delete(1.0, tk.END)
            self.text.insert(tk.END, f"Error: {e}")
            
        # Schedule next update
        self.frame.after(self.refresh_interval * 1000, self.update_content)

class ImageWidget(GeekletWidget):
    """Image widget (like inspire image geeklet)"""
    def __init__(self, root, x, y, width, height, image_path, opacity=0.18):
        super().__init__(root, x, y, width, height, opacity)
        self.image_path = image_path
        
        # Image label
        self.image_label = tk.Label(self.frame, bg='black')
        self.image_label.pack(fill=tk.BOTH, expand=True)
        
        self.load_image()
        
    def load_image(self):
        """Load and display image"""
        try:
            if self.image_path.startswith('http'):
                # Download image
                response = requests.get(self.image_path, timeout=10)
                image = Image.open(io.BytesIO(response.content))
            else:
                # Local file
                image = Image.open(self.image_path)
                
            # Resize to fit widget
            widget_size = (
                self.frame.winfo_reqwidth(),
                self.frame.winfo_reqheight()
            )
            image = image.resize(widget_size, Image.Resampling.LANCZOS)
            
            # Convert to PhotoImage
            photo = ImageTk.PhotoImage(image)
            self.image_label.config(image=photo)
            self.image_label.image = photo  # Keep reference
            
        except Exception as e:
            self.image_label.config(text=f"Image Error: {e}", fg='red')

class WebWidget(GeekletWidget):
    """Web widget (like weather webpage geeklet)"""
    def __init__(self, root, x, y, width, height, url, refresh_interval=60, opacity=0.06):
        super().__init__(root, x, y, width, height, opacity)
        self.url = url
        self.refresh_interval = refresh_interval
        
        # Simple web content display
        self.web_label = tk.Label(
            self.frame,
            text="Loading web content...",
            bg='black',
            fg='white',
            font=('Arial', 8),
            justify=tk.LEFT,
            anchor='nw'
        )
        self.web_label.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        
        self.update_web_content()
        
    def update_web_content(self):
        """Fetch web content"""
        try:
            # Simple curl command to get basic content
            result = subprocess.run([
                'curl', '-s', '--max-time', '10', self.url
            ], capture_output=True, text=True)
            
            # Extract useful text (basic parsing)
            content = result.stdout[:500]  # First 500 chars
            if '<title>' in content:
                title_start = content.find('<title>') + 7
                title_end = content.find('</title>')
                if title_end > title_start:
                    title = content[title_start:title_end]
                    self.web_label.config(text=f"📄 {title}")
            else:
                self.web_label.config(text="Web content loaded")
                
        except Exception as e:
            self.web_label.config(text=f"Web Error: {e}")
            
        # Schedule next update
        self.frame.after(self.refresh_interval * 1000, self.update_web_content)

class FileWidget(GeekletWidget):
    """File/log monitor widget"""
    def __init__(self, root, x, y, width, height, file_path, opacity=0.8):
        super().__init__(root, x, y, width, height, opacity)
        self.file_path = file_path
        
        # Text widget for file content
        self.text = tk.Text(
            self.frame,
            bg='black',
            fg='white',
            font=('LucidaGrande', 10),
            wrap=tk.WORD,
            borderwidth=0,
            highlightthickness=0
        )
        self.text.pack(fill=tk.BOTH, expand=True, padx=5, pady=5)
        
        self.update_file_content()
        
    def update_file_content(self):
        """Read and display file content"""
        try:
            if os.path.exists(self.file_path):
                with open(self.file_path, 'r') as f:
                    # Last 20 lines
                    lines = f.readlines()[-20:]
                    content = ''.join(lines)
                    
                self.text.delete(1.0, tk.END)
                self.text.insert(tk.END, content)
            else:
                self.text.delete(1.0, tk.END)
                self.text.insert(tk.END, f"File not found: {self.file_path}")
                
        except Exception as e:
            self.text.delete(1.0, tk.END)
            self.text.insert(tk.END, f"Error reading file: {e}")
            
        # Update every 5 seconds
        self.frame.after(5000, self.update_file_content)

class XRAIGeekletSystem:
    """Main XRAI geeklet system"""
    def __init__(self):
        self.root = tk.Tk()
        self.root.withdraw()  # Hide main window
        self.widgets = []
        self.setup_widgets()
        
    def setup_widgets(self):
        """Create GeekTool-style widgets for XRAI"""
        
        # 1. XRAI Activity Monitor (top-left)
        activity_widget = ShellWidget(
            self.root,
            x=50, y=50, width=300, height=400,
            command="ps -arcwwwxo 'command %cpu %mem' | grep -v grep | head -15",
            refresh_interval=5,
            opacity=0.4
        )
        self.widgets.append(activity_widget)
        
        # 2. XRAI Log Monitor (bottom-left)
        log_widget = FileWidget(
            self.root,
            x=50, y=500, width=300, height=200,
            file_path="/Users/jamestunick/xrai/system_agent.jsonl",
            opacity=0.6
        )
        self.widgets.append(log_widget)
        
        # 3. Network Activity (top-right)
        network_widget = ShellWidget(
            self.root,
            x=1400, y=50, width=400, height=300,
            command="netstat -an | grep ESTABLISHED | head -10",
            refresh_interval=10,
            opacity=0.3
        )
        self.widgets.append(network_widget)
        
        # 4. System Stats (center-right)
        stats_widget = ShellWidget(
            self.root,
            x=1400, y=400, width=400, height=200,
            command="top -l 1 | head -10",
            refresh_interval=3,
            opacity=0.4
        )
        self.widgets.append(stats_widget)
        
        # 5. XRAI Voice Commands (bottom-right)
        voice_widget = ShellWidget(
            self.root,
            x=1400, y=650, width=400, height=300,
            command="tail -20 /Users/jamestunick/Library/Logs/xrai-voice.log | grep '🎤\\|🤖'",
            refresh_interval=2,
            opacity=0.5
        )
        self.widgets.append(voice_widget)
        
        # 6. Inspirational Image (if available)
        try:
            inspire_path = "/Users/jamestunick/Desktop/inspire"
            if os.path.exists(inspire_path):
                # Find first image
                for f in os.listdir(inspire_path):
                    if f.lower().endswith(('.jpg', '.jpeg', '.png')):
                        image_widget = ImageWidget(
                            self.root,
                            x=400, y=700, width=600, height=300,
                            image_path=os.path.join(inspire_path, f),
                            opacity=0.15
                        )
                        self.widgets.append(image_widget)
                        break
        except:
            pass
            
    def keep_in_background(self):
        """Ensure all widgets stay in background"""
        for widget in self.widgets:
            widget.frame.lower()
        self.root.after(10000, self.keep_in_background)  # Every 10 seconds
        
    def run(self):
        """Start the geeklet system"""
        print("🖥️  Starting XRAI GeekTool-style Desktop Overlay...")
        print("📊 Widgets created - check your desktop background")
        
        # Keep widgets in background
        self.keep_in_background()
        
        try:
            self.root.mainloop()
        except KeyboardInterrupt:
            print("\n👋 XRAI Geeklets stopped")

if __name__ == "__main__":
    system = XRAIGeekletSystem()
    system.run()