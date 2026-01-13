import express from 'express';
import path from 'path';
import { fileURLToPath } from 'url';
import { promises as fs } from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = 3001;

app.use(express.json());
app.use(express.static(path.join(__dirname, '../assets')));

// Load pre-analyzed data
app.get('/demo-data', async (req, res) => {
    try {
        const data = await fs.readFile(path.join(__dirname, '../analysis-results.json'), 'utf8');
        res.json(JSON.parse(data));
    } catch (error) {
        res.status(500).json({ error: 'Demo data not found' });
    }
});

// Get specific project data
app.get('/project/:name', async (req, res) => {
    try {
        const data = JSON.parse(await fs.readFile(path.join(__dirname, '../analysis-results.json'), 'utf8'));
        const project = data.find(p => p.name === req.params.name);
        
        if (project) {
            res.json(project);
        } else {
            res.status(404).json({ error: 'Project not found' });
        }
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

app.listen(PORT, () => {
    console.log(`HOLOVIS Demo Server running at http://localhost:${PORT}`);
});