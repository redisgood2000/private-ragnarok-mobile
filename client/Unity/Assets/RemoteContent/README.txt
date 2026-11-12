Private Ragnarok remote content and GRF resources

REMOTE CONTENT PATCHES
Put patchable Unity assets in this folder. The content patch workflow packages
these assets into the Android AssetBundle named "worldcontent".

Required world asset:
- A prefab named exactly WorldRoot. If absent, the patch build generates the
  original procedural town as WorldRoot.
- Optional character replacement: add a prefab named exactly CharacterActor.
  The Android client loads it from the same AssetBundle and uses it instead of
  the procedural placeholder. Keep the root transform at the feet/ground plane.
  The player CharacterController remains on the player root; child colliders are
  removed when the prefab is instantiated.

GRF IMPORT
In the Unity Editor open:
  Private Ragnarok > Import GRF Resources

Choose a locally stored data.grf or rdata.grf, then use the path filter to narrow
the archive (for example "texture/" or "sprite/"). Extract matching resources
into Assets/RemoteContent/GRFImport/<archive-name>/.

Unity-importable formats:
- BMP, TGA, PNG, JPG/JPEG
- WAV, MP3, OGG

Preserved as raw source for a later converter:
- SPR, ACT, PAL
- RSW, GND, GAT, RSM, STR

The importer currently uses the MIT-licensed GRF 0.3.1 reader. It supports GRF
versions 0x102, 0x103 and 0x200. It does not support GRF 0x300 or LZMA-specific
archive variants. The importer has per-file and per-batch extraction limits.

IMPORTANT
- The original GRF archives stay on your PC and are never uploaded by the importer.
- Do not commit or publish proprietary Ragnarok assets unless you have permission
  to redistribute them. Keep extracted assets local/private where required.
- Raw SPR/ACT/map/model source files are not automatically rendered by Unity.
  They need format-specific converters before they can become playable assets.
- To replace the player, create a Unity prefab named CharacterActor using the
  converted/licensed resources, then include it in this folder and publish a
  content patch. Asset-only changes do not need a new APK.
- Do not put C# scripts, native libraries, credentials, or executable code here.
  Those require an APK build.

SPRITE ID REFERENCE CATALOG
The Ai4rei RagnarokOnline Job Class & NPC Sprite List is used as a metadata
reference only:
  https://nn.ai4rei.net/dev/npclist/?qq=8

In the Unity Editor run:
  Private Ragnarok > Create Reference Sprite Catalog

This creates:
  Assets/Resources/PrivateRagnarok/RagnarokSpriteCatalog.asset

The initial catalog contains the 40 names/IDs shown on the referenced page
(IDs 610–649). Entries contain names and IDs only; directional Sprite slots are
empty. To display a character, assign eight authorized/imported Unity Sprite
assets in clockwise order (south, south-west, west, north-west, north,
north-east, east, south-east), or assign a single sprite. An optional Animator
Controller can provide idle/walk/attack animation states.

The player bootstrap checks ID 635 (4_M_SWORDMAN) and uses the sprite actor only
when that catalog entry has at least one visual asset. Otherwise it safely
keeps the existing procedural placeholder. A remote CharacterActor prefab
continues to take priority.

The reference site states that Ragnarok-related graphics are copyrighted by
Gravity Co., Ltd. and Lee Myoungjin. The catalog tool copies metadata only;
it does not scrape, extract, or redistribute website images or game graphics.
Only add art that you own or are licensed to use.
