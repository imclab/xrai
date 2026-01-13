import express from 'express';
import path from 'path';
import { fileURLToPath } from 'url';
import { promises as fs } from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = 3002;

// Enable CORS
app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Headers', 'Content-Type');
    next();
});

// Serve static files
app.use(express.static(path.join(__dirname, 'assets')));

// Serve demo data
app.get('/demo-data', async (req, res) => {
    try {
        const data = await fs.readFile(path.join(__dirname, 'analysis-results.json'), 'utf8');
        res.json(JSON.parse(data));
    } catch (error) {
        console.error('Error loading demo data:', error);
        res.status(500).json({ error: 'Failed to load demo data' });
    }
});

// Root redirect
app.get('/', (req, res) => {
    res.redirect('/demo.html');
});

// Start server
app.listen(PORT, () => {
    console.log(`\n✅ HOLOVIS Demo Server is running!`);
    console.log(`\n🌐 Open your browser and go to:`);
    console.log(`   http://localhost:${PORT}/demo.html`);
    console.log(`\n📊 Available pages:`);
    console.log(`   http://localhost:${PORT}/demo.html - Interactive gallery`);
    console.log(`   http://localhost:${PORT}/test.html - Test page`);
    console.log(`   http://localhost:${PORT}/ - Main visualization`);
    console.log(`\n🛑 Press Ctrl+C to stop the server\n`);
});