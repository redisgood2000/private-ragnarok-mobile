Private Ragnarok remote content patch assets

Put patchable Unity assets in this folder. The content patch build workflow packages
these assets into an Android AssetBundle named "worldcontent".

Required for a world replacement:
- Include a prefab named WorldRoot (the asset name must be exactly WorldRoot).
- The prefab should contain the full environment you want instantiated in the demo.
- Materials and textures should be included as dependencies by Unity's AssetBundle build.
- Keep all shaders/materials compatible with the Android graphics pipeline and test on device.

The patch client verifies SHA-256 before loading the bundle. If the manifest, download,
hash, bundle, or prefab is invalid, the game falls back to the built-in prototype.

Do not put C# scripts, native libraries, Android plugins, credentials, or executable code here.
Those require an APK build and are not supported as remote patches.


The original procedural town is automatically baked into WorldRoot.prefab during the content patch build. Published bundles are stored at update/worldcontent and served from raw.githubusercontent.com; the manifest is enabled only after the bundle commit succeeds.
