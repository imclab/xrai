#!/usr/bin/env python3
import http.server
import socketserver
import os
import json

PORT = 8080

class MyHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory="assets", **kwargs)
    
    def do_GET(self):
        if self.path == '/':
            self.path = '/demo.html'
        elif self.path == '/demo-data':
            try:
                with open('analysis-results.json', 'r') as f:
                    data = f.read()
                self.send_response(200)
                self.send_header('Content-type', 'application/json')
                self.send_header('Access-Control-Allow-Origin', '*')
                self.end_headers()
                self.wfile.write(data.encode())
                return
            except:
                self.send_error(404, "Data file not found")
                return
        
        super().do_GET()

print(f"\n🚀 HOLOVIS Server starting on port {PORT}...")
print(f"\n✨ Open your browser and go to:")
print(f"   http://localhost:{PORT}/demo.html")
print(f"\n📁 Serving files from: {os.getcwd()}/assets")
print(f"\n🛑 Press Ctrl+C to stop the server\n")

with socketserver.TCPServer(("", PORT), MyHTTPRequestHandler) as httpd:
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\n\nServer stopped.")