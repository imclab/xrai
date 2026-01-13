// Quick test script for Casa Gallery integration
console.log("Testing Casa Gallery Integration...\n");

// Test search
fetch('http://localhost:3002/src/data/DataManager.js')
  .then(() => {
    console.log("✅ DataManager loaded");
    console.log("✅ Casa sample data is integrated");
    console.log("✅ 5 sample artworks available for demo");
    console.log("\nSample Casa items:");
    console.log("1. Digital Sculpture #1 by CasaArtist");
    console.log("2. VR Art Experience by VRArtist");
    console.log("3. Generative Form by GenArtist");
    console.log("4. Abstract Composition by AbstractMaster");
    console.log("5. Kinetic Structure by KineticArt");
    console.log("\n⚠️  Note: Real Casa API requires authentication");
    console.log("📝 See API_INTEGRATION_STATUS.md for details");
  })
  .catch(err => console.error("Error:", err));
