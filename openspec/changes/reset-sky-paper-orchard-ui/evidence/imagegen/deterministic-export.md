# Deterministic Art Export Evidence

The owned exporter was run twice without changing any reviewed source between runs. All 57 checked outputs were byte-identical on the second run: 54 runtime PNGs, the 56-binding manifest, the serialized ArtSet asset, and the review gallery.

- active ArtSet: `sunny-orchard-painted@2`
- semantic bindings: 56
- unique runtime PNGs: 54
- screen-background source SHA-256: `5a75fc83df74ce15f6c975743564a6ff3bc8d364ee9922d19cda301b5a2039a6`
- screen-background runtime SHA-256: `70692572f1b8447a8307527b155fe35f1666bcb087915cb175b1159bbf690aff`
- screen-background runtime GUID: `5fb878b839d12d1f37a65b2679a46488`
- corner-ornament source SHA-256: `6c32743e24537f90860b1151609938f9a625d6aa3cf770d7d7ab0a1b3c633ec9`
- corner-ornament runtime SHA-256: `833b22e57920604609ebcd4af09cb90823e8a00fef849b99c5923f407003f512`
- corner-ornament runtime GUID: `8b29f204b254f88d5ff6a91bc1997cc1`
- manifest SHA-256: `3331932e549f1435e599233788e3af842baa0da85d6c3146ae131862cea04075`
- ArtSet asset SHA-256: `4bf65d535c2297f247760ca07ef2768f9d90934a5394ee0976329c35bc4175dc`

`git status --short` reports no modified `.meta` file in the active source or runtime ArtSet directories, so the exporter preserved existing destination GUID/import geometry.
