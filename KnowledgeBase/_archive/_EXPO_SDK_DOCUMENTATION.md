# Expo & React Native Documentation Reference

**Project**: Expo SDK 54, React Native 0.81.5 | **Updated**: 2026-01-14

---

## Expo

**Core**: [Main Docs](https://docs.expo.dev/) | [LLM-Friendly](https://docs.expo.dev/llms-full.txt) | [Glossary](https://docs.expo.dev/more/glossary-of-terms/)

**GitHub**: [expo](https://github.com/expo) | [Examples](https://github.com/expo/examples) | [FYI](https://github.com/expo/fyi)

**Workflow**: [Overview](https://docs.expo.dev/workflow/overview/) | [Using Libraries](https://docs.expo.dev/workflow/using-libraries/) | [Tools](https://docs.expo.dev/develop/tools/) | [Splash/Icons](https://docs.expo.dev/develop/user-interface/splash-screen-and-app-icon/)

**CLI/Config**: [Environment Variables](https://docs.expo.dev/guides/environment-variables/) | [Expo CLI](https://docs.expo.dev/more/expo-cli/#compiling) | [Create Expo](https://docs.expo.dev/more/create-expo/)

**Runtime**: [Hermes](https://docs.expo.dev/guides/using-hermes/) | [Bun](https://docs.expo.dev/guides/using-bun/) | [Local-First](https://docs.expo.dev/guides/local-first/)

**SDK Modules**: [Sensors](https://docs.expo.dev/versions/latest/sdk/sensors/) | [SMS](https://docs.expo.dev/versions/latest/sdk/sms/) | [Speech](https://docs.expo.dev/versions/latest/sdk/speech/) | [SwiftUI](https://docs.expo.dev/versions/latest/sdk/ui/swift-ui/) | [SwiftUI Guide](https://docs.expo.dev/guides/expo-ui-swift-ui/) | [MapView](https://docs.expo.dev/versions/latest/sdk/map-view/)

**EAS**: [Environment Variables](https://docs.expo.dev/eas/environment-variables/) | [Build Config](https://docs.expo.dev/build-reference/build-configuration/) | [MCP Integration](https://docs.expo.dev/eas/ai/mcp/) | [Automating CLI](https://docs.expo.dev/eas/workflows/automating-eas-cli/) | [Workflows](https://docs.expo.dev/eas/workflows/examples/introduction/) | [E2E Tests](https://docs.expo.dev/eas/workflows/examples/e2e-tests/)

**iOS Build/Submit**: [iOS Builds](https://docs.expo.dev/build-reference/ios-builds/) | [TestFlight](https://docs.expo.dev/build-reference/npx-testflight/) | [Submit](https://docs.expo.dev/submit/ios/) | [Credentials](https://docs.expo.dev/app-signing/existing-credentials/)

**Testing/Monitoring**: [Unit Testing](https://docs.expo.dev/develop/unit-testing/) | [Monitoring](https://docs.expo.dev/monitoring/services/) | [Sentry](https://docs.expo.dev/guides/using-sentry/) | [Web Deploy](https://docs.expo.dev/deploy/web/)

**Tutorials**: [EAS Intro](https://docs.expo.dev/tutorial/eas/introduction/) | [GitHub](https://docs.expo.dev/tutorial/eas/using-github/) | [Next Steps](https://docs.expo.dev/tutorial/eas/next-steps/) | [YouTube Playlist](https://www.youtube.com/playlist?list=PLsXDmrmFV_AS14tZCBin6m9NIS_VCUKe2)

**Local-First**: [Guide](https://docs.expo.dev/guides/local-first/) | [Legend State + Supabase](https://github.com/expo/examples/tree/master/with-legend-state-supabase) | [Blog](https://supabase.com/blog/local-first-expo-legend-state)

**Team**: [Snacks](https://expo.dev/@jt5d?tab=snacks) | [Frowning Green Salsa](https://snack.expo.dev/@jt5d/frowning-green-salsa?platform=ios)

---

## React Native

**Core**: [Metro](https://reactnative.dev/docs/metro) | [Libraries](https://reactnative.dev/docs/libraries) | [Debugging](https://reactnative.dev/docs/debugging) | [Profiling](https://reactnative.dev/docs/profiling)

**Performance**: [Build Speed](https://reactnative.dev/docs/build-speed) | [FlatList](https://reactnative.dev/docs/optimizing-flatlist-configuration) | [Hot Reloading](https://reactnative.dev/blog/2016/03/24/introducing-hot-reloading) | [RN 0.83 Web Perf](https://reactnative.dev/blog/2025/12/10/react-native-0.83#web-performance-apis-as-stable)

**Libraries**: [RN Directory](https://reactnative.directory/) | [Gesture Handler](https://docs.swmansion.com/react-native-gesture-handler/docs/) | [React Virtualized](https://github.com/bvaughn/react-virtualized)

**Showcase**: [RN Apps](https://github.com/ReactNativeNews/React-Native-Apps) | [Notion Clone](https://github.com/betomoedano/React-Native-Notion-Clone) | [Expo UI Playground](https://github.com/betomoedano/expo-ui-playground)

---

## VisionOS / Spatial

**Callstack**: [react-native-visionos](https://github.com/callstack/react-native-visionos) | [Docs](https://oss.callstack.com/react-native-visionos-docs/category/getting-started/) | [Immersive Spaces](https://oss.callstack.com/react-native-visionos-docs/docs/guides/immersive-spaces)

**Rock.js**: [GitHub](https://github.com/callstackincubator/rock) | [iOS Brownfield](https://www.rockjs.dev/docs/brownfield/ios)

---

## 3D/VFX Examples

**CodeSandbox**: [GPGPU Curl Noise](https://codesandbox.io/p/sandbox/gpgpu-curl-noise-dof-forked-vluse3) | [Dynamic EnvMaps](https://codesandbox.io/p/sandbox/building-dynamic-envmaps-forked-5c74vy)

---

## Usage Notes

```bash
# LLM-friendly docs (full context)
curl https://docs.expo.dev/llms-full.txt

# portals_v4 builds
./scripts/build_minimal.sh          # Local dev (react-unity)
eas build --platform ios            # EAS build
npx expo submit:ios                 # TestFlight
```

---

**Related KB**: `_REACT_NATIVE_UNITY_PACKAGES.md` | `_UNITY_AS_A_LIBRARY_IOS.md`
