import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { OBJLoader } from 'three/addons/loaders/OBJLoader.js';
import { FBXLoader } from 'three/addons/loaders/FBXLoader.js';
import { RGBELoader } from 'three/addons/loaders/RGBELoader.js';
import TWEEN from '@tweenjs/tween.js';
import axios from 'axios';
import { inflate } from 'pako';

const MODES = {
  STARS: 'stars',
  GITHUB: 'github',
  OBJAVERSE: 'objaverse',
  RERUN: 'rerun',
  ICOSA: 'icosa',
  NEEDLE: 'needle'
};

const state = {
  mode: MODES.STARS,
  scene: null,
  camera: null,
  renderer: null,
  controls: null,
  starGroup: null,
  githubGroup: null,
  objaverseGroup: null,
  searchGroup: null,
  modelsGroup: null,
  mixers: [],
  mixersActive: false,
  clock: new THREE.Clock(),
  localDataset: null,
  latestArtwork: null,
  lastSearchResults: null,
  currentRerunUrl: 'https://rerun.io/viewer',
  currentIcosaUrl: 'https://gallery.icosa.foundation/embed',
  currentNeedleUrl: 'https://engine.needle.tools/samples-uploads/screensharing/?room=329395'
};

const dom = {};

function cacheDomReferences() {
  dom.loadingOverlay = document.getElementById('loading-overlay');
  dom.loadingText = document.getElementById('loading-text');
  dom.canvasContainer = document.getElementById('canvas-container');
  dom.iframeContainer = document.getElementById('iframe-container');
  dom.embeddedViewer = document.getElementById('embedded-viewer');
  dom.stats = document.getElementById('stats');
  dom.legend = document.getElementById('legend');
  dom.artworkInfo = document.getElementById('artwork-info');
  dom.objaverseSearch = document.getElementById('objaverse-search');
  dom.jsonUpload = document.getElementById('json-upload');
  dom.modelUpload = document.getElementById('model-upload');
  dom.githubDate = document.getElementById('github-date');
  dom.searchTerm = document.getElementById('search-term');
  dom.searchSource = document.getElementById('search-source');
  dom.layoutStyle = document.getElementById('layout-style');
  dom.rerunUrl = document.getElementById('rerun-url');
}

function setLoading(message, visible = true) {
  if (!dom.loadingOverlay) return;
  if (message) dom.loadingText.textContent = message;
  dom.loadingOverlay.classList.toggle('hidden', !visible);
}

async function createRenderer() {
  let renderer;
  if (navigator.gpu) {
    const { default: WebGPURenderer } = await import('three/addons/renderers/webgpu/WebGPURenderer.js');
    renderer = new WebGPURenderer({ antialias: true, alpha: true });
    renderer.setSize(dom.canvasContainer.clientWidth, dom.canvasContainer.clientHeight);
    dom.canvasContainer.appendChild(renderer.domElement);
    await renderer.init();
  } else {
    renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.setSize(dom.canvasContainer.clientWidth, dom.canvasContainer.clientHeight);
    renderer.outputColorSpace = THREE.SRGBColorSpace;
    dom.canvasContainer.appendChild(renderer.domElement);
  }
  renderer.setAnimationLoop(renderLoop);
  return renderer;
}

async function initScene() {
  state.scene = new THREE.Scene();
  state.scene.background = new THREE.Color(0x000006);

  state.camera = new THREE.PerspectiveCamera(
    60,
    dom.canvasContainer.clientWidth / dom.canvasContainer.clientHeight,
    0.1,
    2000
  );
  state.camera.position.set(0, 42, 165);

  state.renderer = await createRenderer();

  state.controls = new OrbitControls(state.camera, state.renderer.domElement);
  state.controls.enableDamping = true;
  state.controls.dampingFactor = 0.045;
  state.controls.minDistance = 12;
  state.controls.maxDistance = 650;

  const ambient = new THREE.AmbientLight(0xa0b9ff, 1.15);
  state.scene.add(ambient);

  const key = new THREE.DirectionalLight(0x88c6ff, 1.25);
  key.position.set(40, 80, 40);
  state.scene.add(key);

  const rim = new THREE.PointLight(0x3355ff, 1.6, 420, 2);
  rim.position.set(-90, -40, -120);
  state.scene.add(rim);

  await buildStarField();
  await buildGithubLayer();
  await buildObjaverseLayer();

  switchMode(MODES.STARS);
  setLoading('', false);
}

async function buildStarField() {
  const response = await fetch('./assets/star_catalog.json');
  const stars = await response.json();

  const positions = new Float32Array(stars.length * 3);
  const colors = new Float32Array(stars.length * 3);
  const color = new THREE.Color();

  stars.forEach((star, i) => {
    const i3 = i * 3;
    positions[i3 + 0] = star.x;
    positions[i3 + 1] = star.y;
    positions[i3 + 2] = star.z;
    color.set(star.color || '#9bdcff');
    colors[i3 + 0] = color.r;
    colors[i3 + 1] = color.g;
    colors[i3 + 2] = color.b;
  });

  const geometry = new THREE.BufferGeometry();
  geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
  geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));

  const material = new THREE.PointsMaterial({
    vertexColors: true,
    size: 3.5,
    sizeAttenuation: true,
    transparent: true,
    opacity: 0.9,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });

  const points = new THREE.Points(geometry, material);
  state.starGroup = new THREE.Group();
  state.starGroup.add(points);

  const halo = new THREE.Mesh(
    new THREE.SphereGeometry(400, 64, 64),
    new THREE.ShaderMaterial({
      side: THREE.BackSide,
      transparent: true,
      uniforms: {
        glowColor: { value: new THREE.Color(0x1c3d6f) },
        intensity: { value: 0.6 }
      },
      vertexShader: `
        varying vec3 vNormal;
        void main() {
          vNormal = normalize(normalMatrix * normal);
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        varying vec3 vNormal;
        uniform vec3 glowColor;
        uniform float intensity;
        void main() {
          float strength = pow(0.6 - dot(vNormal, vec3(0.0, 0.0, 1.0)), 3.0);
          gl_FragColor = vec4(glowColor, strength * intensity);
        }
      `
    })
  );
  state.starGroup.add(halo);
  state.scene.add(state.starGroup);

  dom.legend.innerHTML = `
    <h4>Legend</h4>
    <ul>
      <li><span class="swatch swatch-hot"></span> Hot stars</li>
      <li><span class="swatch swatch-cool"></span> Cool stars</li>
      <li><span class="swatch swatch-neutral"></span> Sun-like</li>
    </ul>`;
}

async function buildGithubLayer(defaultData) {
  const data = defaultData || await fetch('./data/sample_github.json').then(r => r.json());
  if (state.githubGroup) {
    state.scene.remove(state.githubGroup);
  }
  state.githubGroup = new THREE.Group();

  const repoNodes = new Map();
  const actorNodes = new Map();

  data.forEach(event => {
    const repo = event.repo;
    const actor = event.actor;
    if (!repoNodes.has(repo)) {
      repoNodes.set(repo, {
        id: repo,
        type: 'repo',
        stars: event.stars || Math.random() * 800,
        language: event.language || 'Unknown'
      });
    }
    if (!actorNodes.has(actor)) {
      actorNodes.set(actor, {
        id: actor,
        type: 'actor',
        repo
      });
    }
  });

  const repoArray = Array.from(repoNodes.values());
  const actorArray = Array.from(actorNodes.values());

  const radiusRepo = 45;
  const radiusActor = 65;

  repoArray.forEach((repo, index) => {
    const angle = (index / repoArray.length) * Math.PI * 2;
    repo.position = new THREE.Vector3(
      Math.cos(angle) * radiusRepo,
      (Math.random() - 0.5) * 12,
      Math.sin(angle) * radiusRepo
    );
  });

  actorArray.forEach((actor, index) => {
    const angle = (index / actorArray.length) * Math.PI * 2;
    actor.position = new THREE.Vector3(
      Math.cos(angle) * radiusActor,
      (Math.random() - 0.5) * 18,
      Math.sin(angle) * radiusActor
    );
  });

  const repoMaterial = new THREE.MeshStandardMaterial({
    color: 0x7f9cff,
    emissive: 0x1f3dff,
    emissiveIntensity: 0.55,
    transparent: true,
    opacity: 0.96
  });

  repoArray.forEach(repo => {
    const size = THREE.MathUtils.clamp(Math.log10(repo.stars + 10), 1.4, 4.0);
    const mesh = new THREE.Mesh(new THREE.SphereGeometry(size, 24, 24), repoMaterial.clone());
    mesh.position.copy(repo.position);
    mesh.userData = repo;
    state.githubGroup.add(mesh);
  });

  const actorMaterial = new THREE.MeshStandardMaterial({
    color: 0xffb347,
    emissive: 0x6f3b15,
    emissiveIntensity: 0.4,
    metalness: 0.12,
    roughness: 0.35
  });

  actorArray.forEach(actor => {
    const mesh = new THREE.Mesh(new THREE.OctahedronGeometry(1.7), actorMaterial.clone());
    mesh.position.copy(actor.position);
    mesh.userData = actor;
    state.githubGroup.add(mesh);

    const repo = repoNodes.get(actor.repo);
    if (repo) {
      const points = new THREE.BufferGeometry().setFromPoints([actor.position, repo.position]);
      const lineMaterial = new THREE.LineBasicMaterial({
        color: 0x80f7ff,
        transparent: true,
        opacity: 0.32
      });
      const line = new THREE.Line(points, lineMaterial);
      state.githubGroup.add(line);
    }
  });

  state.scene.add(state.githubGroup);
}

async function buildObjaverseLayer() {
  state.objaverseGroup = new THREE.Group();
  state.scene.add(state.objaverseGroup);

  const fallback = new THREE.Mesh(
    new THREE.IcosahedronGeometry(6, 2),
    new THREE.MeshStandardMaterial({ color: 0x5566ff, wireframe: true, transparent: true, opacity: 0.35 })
  );
  fallback.name = 'ObjaverseFallback';
  fallback.position.set(0, 5, 0);
  state.objaverseGroup.add(fallback);

  new RGBELoader()
    .setPath('https://threejs.org/examples/textures/equirectangular/')
    .load('royal_esplanade_1k.hdr', texture => {
      texture.mapping = THREE.EquirectangularReflectionMapping;
      state.scene.environment = texture;
    });
}

function toggleCanvas(showCanvas) {
  dom.canvasContainer.style.display = showCanvas ? 'block' : 'none';
  dom.iframeContainer.classList.toggle('hidden', showCanvas);
}

function setIframeSource(url) {
  if (!url) return;
  if (dom.embeddedViewer.src !== url) {
    dom.embeddedViewer.src = url;
  }
}

function switchMode(mode) {
  state.mode = mode;
  document.querySelectorAll('nav button').forEach(btn => btn.classList.remove('active'));

  if (mode === MODES.RERUN) {
    document.getElementById('mode-rerun').classList.add('active');
    toggleCanvas(false);
    setIframeSource(state.currentRerunUrl);
    return;
  }
  if (mode === MODES.ICOSA) {
    document.getElementById('mode-icosa').classList.add('active');
    toggleCanvas(false);
    setIframeSource(state.currentIcosaUrl);
    return;
  }
  if (mode === MODES.NEEDLE) {
    document.getElementById('mode-needle').classList.add('active');
    toggleCanvas(false);
    setIframeSource(state.currentNeedleUrl);
    return;
  }

  toggleCanvas(true);

  switch (mode) {
    case MODES.STARS:
      document.getElementById('mode-stars').classList.add('active');
      toggleGroupVisibility(state.starGroup, true);
      toggleGroupVisibility(state.githubGroup, false);
      toggleGroupVisibility(state.objaverseGroup, false);
      toggleGroupVisibility(state.searchGroup, true);
      break;
    case MODES.GITHUB:
      document.getElementById('mode-gh').classList.add('active');
      toggleGroupVisibility(state.starGroup, false);
      toggleGroupVisibility(state.githubGroup, true);
      toggleGroupVisibility(state.objaverseGroup, false);
      toggleGroupVisibility(state.searchGroup, true);
      break;
    case MODES.OBJAVERSE:
      document.getElementById('mode-objaverse').classList.add('active');
      toggleGroupVisibility(state.starGroup, false);
      toggleGroupVisibility(state.githubGroup, false);
      toggleGroupVisibility(state.objaverseGroup, true);
      toggleGroupVisibility(state.searchGroup, true);
      break;
  }
}

function toggleGroupVisibility(group, visible) {
  if (!group) return;
  group.visible = visible;
  group.traverse(child => { child.visible = visible; });
}

function renderLoop() {
  const delta = state.clock.getDelta();
  TWEEN.update();
  state.controls?.update();

  if (state.mixersActive) {
    state.mixers.forEach(mixer => mixer.update(delta));
  }

  if (state.renderer && state.scene && state.camera) {
    state.renderer.render(state.scene, state.camera);
  }
}

async function loadGithubArchive(dateString) {
  if (!dateString) throw new Error('No date provided');
  const url = `https://data.gharchive.org/${dateString}-0.json.gz`;
  setLoading('Fetching GH Archive slice...');

  try {
    const response = await axios.get(url, { responseType: 'arraybuffer' });
    const inflated = inflate(new Uint8Array(response.data), { to: 'string' });
    const lines = inflated.trim().split('\n');
    const events = lines.slice(0, 500).map(line => JSON.parse(line));

    const reduced = events.map(evt => ({
      type: evt.type,
      repo: evt.repo?.name || 'unknown/repo',
      actor: evt.actor?.login || 'anonymous',
      language: evt.payload?.pull_request?.base?.repo?.language || evt.payload?.repository?.language || 'Unknown',
      stars: evt.repo?.stars || Math.floor(Math.random() * 5000)
    }));

    await buildGithubLayer(reduced);
    switchMode(MODES.GITHUB);
  } catch (error) {
    console.error('Failed to fetch GHArchive', error);
    alert('Failed to fetch GHArchive data. Falling back to bundled sample.');
    await buildGithubLayer();
    switchMode(MODES.GITHUB);
  } finally {
    setLoading('', false);
  }
}

async function fetchIcosaArtwork() {
  setLoading('Requesting Icosa artwork...');
  try {
    const response = await axios.get('https://gallery.icosa.foundation/api/artworks/random', { timeout: 8000 });
    const artwork = response.data?.artwork || response.data || null;
    if (!artwork) throw new Error('No artwork returned');

    state.latestArtwork = artwork;
    dom.artworkInfo.innerHTML = `
      <strong>${artwork.title || 'Untitled'}</strong><br/>
      by ${artwork.author || 'Unknown'}<br/>
      ${(artwork.description || '').slice(0, 200)}
    `;

    if (artwork.previewUrl) {
      addArtworkBillboard(artwork.previewUrl);
    }
    state.currentIcosaUrl = artwork.embedUrl || `https://gallery.icosa.foundation/artworks/${artwork.slug || artwork.id}/embed`;
  } catch (err) {
    console.warn('Icosa fetch failed', err);
    dom.artworkInfo.textContent = 'Icosa API unavailable. Try again later.';
  } finally {
    setLoading('', false);
  }
}

function addArtworkBillboard(url) {
  const loader = new THREE.TextureLoader();
  loader.load(url, texture => {
    const aspect = texture.image.height / texture.image.width;
    const geometry = new THREE.PlaneGeometry(20, 20 * aspect);
    const material = new THREE.MeshBasicMaterial({ map: texture, transparent: true, opacity: 0 });
    const plane = new THREE.Mesh(geometry, material);
    plane.position.set(0, 25, -60);
    plane.rotation.y = Math.PI / 8;
    state.scene.add(plane);

    new TWEEN.Tween(material)
      .to({ opacity: 1 }, 1200)
      .start();
  });
}

async function loadObjaverse(query) {
  setLoading('Searching Objaverse...');
  try {
    const response = await axios.get('https://objaverse-api.allenai.org/search', {
      params: { q: query || 'astronaut', limit: 1 }
    });
    const items = response.data?.items || [];
    if (!items.length) throw new Error('No Objaverse results');
    const item = items[0];
    const assetUrl = item.assets?.find(a => a.format === 'glb')?.url || item.previewAsset?.url;
    if (!assetUrl) throw new Error('No GLB asset available');

    removeObjaverseModels();

    const loader = new GLTFLoader();
    loader.load(assetUrl, gltf => {
      const model = gltf.scene;
      model.name = 'ObjaverseModel';
      model.scale.setScalar(30);
      model.position.set(0, -15, 0);
      state.objaverseGroup.add(model);

      if (gltf.animations.length) {
        const mixer = new THREE.AnimationMixer(model);
        mixer.clipAction(gltf.animations[0]).play();
        state.mixers.push(mixer);
        state.mixersActive = true;
      }

      switchMode(MODES.OBJAVERSE);
    });
  } catch (error) {
    console.error('Objaverse fetch failed', error);
    alert('Objaverse lookup failed. Ensure you have connectivity or try another query.');
  } finally {
    setLoading('', false);
  }
}

function removeObjaverseModels() {
  if (!state.objaverseGroup) return;
  const toRemove = state.objaverseGroup.children.filter(child => child.name === 'ObjaverseModel');
  toRemove.forEach(child => {
    state.objaverseGroup.remove(child);
    child.traverse(obj => {
      if (obj.isMesh) {
        obj.geometry?.dispose();
        if (Array.isArray(obj.material)) obj.material.forEach(mat => mat.dispose());
        else obj.material?.dispose();
      }
    });
  });
}

async function runSearch() {
  const term = dom.searchTerm.value.trim();
  if (!term && dom.searchSource.value !== 'local') {
    alert('Enter a search term.');
    return;
  }

  setLoading('Running search...');
  try {
    let results = [];
    switch (dom.searchSource.value) {
      case 'github':
        results = await fetchFromGithub(term);
        break;
      case 'icosa':
        results = await fetchFromIcosa(term);
        break;
      case 'objaverse':
        results = await fetchFromObjaverse(term);
        break;
      case 'local':
        if (!state.localDataset) {
          alert('Load a JSON file first.');
          return;
        }
        results = [].concat(state.localDataset);
        break;
    }

    state.lastSearchResults = results;
    renderSearchResults(results, dom.layoutStyle.value);
    switchMode(MODES.STARS);
  } catch (error) {
    console.error('Search failed', error);
    alert('Search failed. Please try again later.');
  } finally {
    setLoading('', false);
  }
}

async function fetchFromGithub(term) {
  const response = await axios.get('https://api.github.com/search/repositories', {
    params: { q: term, per_page: 24 }
  });
  return (response.data.items || []).map(repo => ({
    id: repo.id,
    label: repo.full_name,
    value: repo.stargazers_count,
    url: repo.html_url,
    type: 'github',
    language: repo.language || 'Unknown'
  }));
}

async function fetchFromIcosa(term) {
  const response = await axios.get('https://gallery.icosa.foundation/api/artworks/search', {
    params: { query: term }
  });
  const artworks = response.data?.results || response.data?.artworks || [];
  return artworks.slice(0, 24).map(item => ({
    id: item.id || item.slug,
    label: item.title || 'Untitled',
    value: item.likes || Math.floor(Math.random() * 300) + 50,
    url: item.url || `https://gallery.icosa.foundation/artworks/${item.slug || item.id}`,
    previewUrl: item.previewUrl,
    type: 'icosa'
  }));
}

async function fetchFromObjaverse(term) {
  const response = await axios.get('https://objaverse-api.allenai.org/search', {
    params: { q: term, limit: 24 }
  });
  const items = response.data?.items || [];
  return items.map(item => ({
    id: item.uid || item.id,
    label: item.title || item.name || 'Objaverse asset',
    value: item.score || Math.floor(Math.random() * 100) + 10,
    url: item.assets?.find(a => a.format === 'glb')?.url,
    type: 'objaverse'
  }));
}

function renderSearchResults(results, layoutStyle) {
  if (state.searchGroup) {
    state.scene.remove(state.searchGroup);
  }
  state.searchGroup = new THREE.Group();
  state.searchGroup.name = 'SearchResults';
  state.scene.add(state.searchGroup);

  if (!Array.isArray(results) || !results.length) {
    dom.stats.innerHTML = '<em>No results to display.</em>';
    return;
  }

  if (layoutStyle === 'city') {
    renderCityLayout(results);
  } else {
    renderHypergraphLayout(results);
  }

  updateStatsPanel(results);
}

function renderHypergraphLayout(results) {
  const hubMaterial = new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.25 });
  const hub = new THREE.Mesh(new THREE.SphereGeometry(4, 24, 24), hubMaterial);
  state.searchGroup.add(hub);

  const radius = 55;
  const colors = {
    github: 0x80d8ff,
    icosa: 0xff9cf0,
    objaverse: 0xb5ff6b,
    local: 0xf8f184
  };

  results.forEach((item, index) => {
    const angle = (index / results.length) * Math.PI * 2;
    const y = (Math.random() - 0.5) * 24;
    const position = new THREE.Vector3(
      Math.cos(angle) * radius,
      y,
      Math.sin(angle) * radius
    );

    const size = THREE.MathUtils.clamp(Math.log(item.value + 5), 1.2, 4.5);
    const material = new THREE.MeshStandardMaterial({
      color: colors[item.type] || 0xffffff,
      emissive: colors[item.type] || 0x8bc1ff,
      emissiveIntensity: 0.4,
      transparent: true,
      opacity: 0.9
    });
    const node = new THREE.Mesh(new THREE.IcosahedronGeometry(size, 1), material);
    node.position.copy(position);
    node.userData = item;
    state.searchGroup.add(node);

    const lineGeometry = new THREE.BufferGeometry().setFromPoints([position, new THREE.Vector3(0, 0, 0)]);
    const lineMaterial = new THREE.LineDashedMaterial({
      color: material.color,
      dashSize: 3,
      gapSize: 1.5,
      transparent: true,
      opacity: 0.4
    });
    const line = new THREE.Line(lineGeometry, lineMaterial);
    line.computeLineDistances();
    state.searchGroup.add(line);
  });
}

function renderCityLayout(results) {
  const spacing = 10;
  const gridSize = Math.ceil(Math.sqrt(results.length));
  const offset = ((gridSize - 1) * spacing) / 2;
  const base = new THREE.Mesh(
    new THREE.PlaneGeometry(gridSize * spacing + 20, gridSize * spacing + 20),
    new THREE.MeshStandardMaterial({ color: 0x0f1a2f, transparent: true, opacity: 0.65 })
  );
  base.rotation.x = -Math.PI / 2;
  base.receiveShadow = true;
  state.searchGroup.add(base);

  results.forEach((item, index) => {
    const gx = index % gridSize;
    const gz = Math.floor(index / gridSize);
    const height = THREE.MathUtils.clamp(Math.log(item.value + 10) * 6, 3, 45);
    const geometry = new THREE.BoxGeometry(spacing * 0.65, height, spacing * 0.65);
    const material = new THREE.MeshStandardMaterial({
      color: pickCityColor(item.type),
      emissive: pickCityColor(item.type),
      emissiveIntensity: 0.25,
      metalness: 0.25,
      roughness: 0.35
    });
    const building = new THREE.Mesh(geometry, material);
    building.position.set(gx * spacing - offset, height / 2, gz * spacing - offset);
    building.userData = item;
    state.searchGroup.add(building);
  });
}

function pickCityColor(type) {
  switch (type) {
    case 'github':
      return 0x0099ff;
    case 'icosa':
      return 0xff7beb;
    case 'objaverse':
      return 0x83ff4f;
    case 'local':
      return 0xffe66b;
    default:
      return 0xffffff;
  }
}

function updateStatsPanel(results) {
  const byType = results.reduce((acc, item) => {
    acc[item.type] = (acc[item.type] || 0) + 1;
    return acc;
  }, {});

  const maxValue = results.reduce((max, item) => Math.max(max, item.value || 0), 0);

  dom.stats.innerHTML = `
    <strong>Total Results:</strong> ${results.length}<br/>
    <strong>Max Value:</strong> ${Math.round(maxValue)}<br/>
    <strong>Breakdown:</strong>
    <ul>
      ${Object.entries(byType).map(([type, count]) => `<li>${type}: ${count}</li>`).join('')}
    </ul>
  `;
}

function loadModelFile(file) {
  const url = URL.createObjectURL(file);
  const extension = file.name.split('.').pop().toLowerCase();

  const clearLoaderState = () => URL.revokeObjectURL(url);

  const onValue = object => {
    if (!state.modelsGroup) {
      state.modelsGroup = new THREE.Group();
      state.modelsGroup.name = 'LoadedModels';
      state.scene.add(state.modelsGroup);
    }

    object.name = `Imported_${file.name}`;
    object.position.set(0, 0, 0);
    object.traverse(child => {
      if (child.isMesh) {
        child.castShadow = true;
        child.receiveShadow = true;
      }
    });
    state.modelsGroup.add(object);
    clearLoaderState();
    alert(`Loaded model: ${file.name}`);
  };

  const onError = error => {
    console.error('Model loading failed', error);
    alert('Failed to load model. Ensure the format is supported.');
    clearLoaderState();
  };

  if (extension === 'glb' || extension === 'gltf') {
    new GLTFLoader().load(url, gltf => onValue(gltf.scene.clone()), undefined, onError);
  } else if (extension === 'obj') {
    new OBJLoader().load(url, obj => onValue(obj), undefined, onError);
  } else if (extension === 'fbx') {
    new FBXLoader().load(url, obj => onValue(obj), undefined, onError);
  } else {
    alert('Unsupported model type. Use .glb, .gltf, .obj, or .fbx');
    clearLoaderState();
  }
}

function setupLegendStyles() {
  const style = document.createElement('style');
  style.textContent = `
    .swatch { display: inline-block; width: 12px; height: 12px; border-radius: 999px; margin-right: 8px; }
    .swatch-hot { background: linear-gradient(120deg, #64c2ff, #a9e3ff); }
    .swatch-cool { background: linear-gradient(120deg, #ff9b73, #ffd0a3); }
    .swatch-neutral { background: linear-gradient(120deg, #ffeead, #f5f1d9); }
  `;
  document.head.appendChild(style);
}

function setupEventListeners() {
  document.getElementById('mode-stars').addEventListener('click', () => switchMode(MODES.STARS));
  document.getElementById('mode-gh').addEventListener('click', () => switchMode(MODES.GITHUB));
  document.getElementById('mode-objaverse').addEventListener('click', () => switchMode(MODES.OBJAVERSE));
  document.getElementById('mode-rerun').addEventListener('click', () => {
    if (!state.currentRerunUrl) state.currentRerunUrl = 'https://rerun.io/viewer';
    switchMode(MODES.RERUN);
  });
  document.getElementById('mode-icosa').addEventListener('click', () => {
    if (!state.currentIcosaUrl) state.currentIcosaUrl = 'https://gallery.icosa.foundation/embed';
    switchMode(MODES.ICOSA);
  });
  document.getElementById('mode-needle').addEventListener('click', () => {
    switchMode(MODES.NEEDLE);
  });

  document.getElementById('load-github').addEventListener('click', async () => {
    const date = dom.githubDate.value;
    if (!date) {
      alert('Choose a date in UTC (YYYY-MM-DD).');
      return;
    }
    await loadGithubArchive(date);
  });

  dom.jsonUpload.addEventListener('change', async event => {
    const file = event.target.files[0];
    if (!file) return;
    try {
      const text = await file.text();
      const json = JSON.parse(text);
      if (!Array.isArray(json)) throw new Error('Expected an array');
      state.localDataset = json;
      alert(`Loaded ${json.length} records from local JSON.`);
    } catch (error) {
      alert('Invalid JSON file.');
    }
  });

  dom.modelUpload.addEventListener('change', event => {
    const file = event.target.files[0];
    if (!file) return;
    loadModelFile(file);
  });

  document.getElementById('fetch-artwork').addEventListener('click', fetchIcosaArtwork);
  document.getElementById('load-objaverse').addEventListener('click', () => loadObjaverse(dom.objaverseSearch.value));
  document.getElementById('load-rerun').addEventListener('click', () => {
    const url = dom.rerunUrl.value.trim() || 'https://rerun.io/viewer';
    state.currentRerunUrl = url;
    switchMode(MODES.RERUN);
  });
  document.getElementById('open-icosa-embed').addEventListener('click', () => {
    if (!state.latestArtwork) {
      alert('Fetch an Icosa artwork first.');
      return;
    }
    state.currentIcosaUrl = state.latestArtwork.embedUrl || `https://gallery.icosa.foundation/artworks/${state.latestArtwork.slug || state.latestArtwork.id}/embed`;
    switchMode(MODES.ICOSA);
  });
  document.getElementById('load-needle').addEventListener('click', () => {
    const room = document.getElementById('needle-room').value.trim() || '329395';
    state.currentNeedleUrl = `https://engine.needle.tools/samples-uploads/screensharing/?room=${encodeURIComponent(room)}`;
    switchMode(MODES.NEEDLE);
  });

  document.getElementById('run-search').addEventListener('click', runSearch);
  dom.layoutStyle.addEventListener('change', () => {
    if (state.lastSearchResults) {
      renderSearchResults(state.lastSearchResults, dom.layoutStyle.value);
    }
  });

  window.addEventListener('resize', onResize);
}

function onResize() {
  if (!state.renderer || !state.camera) return;
  const { clientWidth, clientHeight } = dom.canvasContainer;
  state.camera.aspect = clientWidth / clientHeight;
  state.camera.updateProjectionMatrix();
  state.renderer.setSize(clientWidth, clientHeight);
}

window.addEventListener('DOMContentLoaded', async () => {
  cacheDomReferences();
  setupLegendStyles();
  setupEventListeners();
  setLoading('Initializing starfield...');
  await initScene();
});
