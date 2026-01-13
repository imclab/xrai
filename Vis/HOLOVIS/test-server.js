const http = require('http');
const fs = require('fs');
const path = require('path');

const server = http.createServer((req, res) => {
    console.log('Request received:', req.url);
    
    if (req.url === '/' || req.url === '/demo.html') {
        const filePath = path.join(__dirname, 'assets', 'demo.html');
        fs.readFile(filePath, (err, content) => {
            if (err) {
                res.writeHead(404);
                res.end('File not found: ' + err.message);
            } else {
                res.writeHead(200, { 'Content-Type': 'text/html' });
                res.end(content);
            }
        });
    } else if (req.url === '/demo-data') {
        const dataPath = path.join(__dirname, 'analysis-results.json');
        fs.readFile(dataPath, (err, content) => {
            if (err) {
                res.writeHead(404);
                res.end('Data not found');
            } else {
                res.writeHead(200, { 'Content-Type': 'application/json' });
                res.end(content);
            }
        });
    } else {
        const filePath = path.join(__dirname, 'assets', req.url);
        fs.readFile(filePath, (err, content) => {
            if (err) {
                res.writeHead(404);
                res.end('Not found');
            } else {
                const ext = path.extname(req.url);
                let contentType = 'text/plain';
                if (ext === '.html') contentType = 'text/html';
                else if (ext === '.js') contentType = 'text/javascript';
                else if (ext === '.css') contentType = 'text/css';
                
                res.writeHead(200, { 'Content-Type': contentType });
                res.end(content);
            }
        });
    }
});

const PORT = 3003;
server.listen(PORT, '127.0.0.1', () => {
    console.log(`Server running at http://127.0.0.1:${PORT}/`);
    console.log(`Open http://localhost:${PORT}/demo.html in your browser`);
});

server.on('error', (err) => {
    console.error('Server error:', err);
});