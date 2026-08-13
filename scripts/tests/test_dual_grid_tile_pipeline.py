import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image

SCRIPT_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(SCRIPT_ROOT))

import dual_grid_tile_pipeline as pipeline


class DualGridTilePipelineTests(unittest.TestCase):
    def test_profiles_emit_complete_unity_brush_descriptors(self):
        profile_root = SCRIPT_ROOT / "dual-grid-profiles"
        expected = {
            "A-grass-soil": ("surface.grass", "surface.soil"),
            "B-stone-water": ("surface.stone-road", "surface.water"),
        }
        for profile_id, surfaces in expected.items():
            profile = pipeline.load_profile(profile_root / f"{profile_id}.json")
            descriptor = pipeline.brush_import_descriptor(profile)
            self.assertEqual("fruit-defense.terrain-brush-import.v2", descriptor["schema"])
            self.assertEqual(profile_id, descriptor["profileId"])
            self.assertEqual(surfaces, (
                descriptor["landformSurfaceId"], descriptor["baseSurfaceId"]
            ))
            self.assertEqual((15, 0), (
                descriptor["foregroundMask"], descriptor["backgroundMask"]
            ))
            self.assertEqual(64, descriptor["runtimeTileSize"])
            self.assertEqual("Runtime64", descriptor["runtimeMaskDirectory"])

    def test_runtime64_uses_declared_high_quality_sampling(self):
        source = Image.new("RGBA", (256, 256))
        source.putdata([
            ((x * 3 + y) & 255, (x + y * 5) & 255, (x ^ y) & 255, 255)
            for y in range(256) for x in range(256)
        ])
        runtime = pipeline.runtime_sample(source, 64, "lanczos")
        self.assertEqual((64, 64), runtime.size)
        self.assertEqual(
            source.resize((64, 64), Image.Resampling.LANCZOS).tobytes(),
            runtime.tobytes(),
        )
        stress = pipeline.runtime_center_sample(source, 32)
        self.assertEqual((32, 32), stress.size)
        self.assertNotEqual(runtime.resize((32, 32)).tobytes(), stress.tobytes())

    def test_static_topology_contracts(self):
        pipeline.validate_static_contract()
        image = pipeline.render_mask_board(
            pipeline.A_MOTHER_MASKS,
            256,
            (255, 71, 121, 255),
            (255, 255, 255, 255),
        )
        pixels = image.tobytes()
        landform = sum(
            pixels[index : index + 4] == bytes((255, 71, 121, 255))
            for index in range(0, len(pixels), 4)
        )
        self.assertEqual(768 * 768, landform)

    def test_center_address_point_normalization(self):
        source = Image.new("RGBA", (3, 3))
        source.putdata([(index, index, index, 255) for index in range(9)])
        normalized = pipeline.normalize_nearest(source, (2, 2))
        self.assertEqual(
            [(0, 0, 0, 255), (2, 2, 2, 255), (6, 6, 6, 255), (8, 8, 8, 255)],
            [normalized.getpixel((x, y)) for y in range(2) for x in range(2)],
        )

    def test_protected_hybrid_uses_whole_source_pixels(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            route_masks = {}
            for mask in range(16):
                route_masks[mask] = Image.new("RGBA", (256, 256), (10, mask, 20, 255))
                Image.new("RGBA", (256, 256), (200, mask, 210, 255)).save(
                    root / f"Mask-{mask:02d}.png"
                )
            profile = {
                "candidateMode": "protected-hybrid",
                "protectedReviewWidth": 32,
                "crossoverWidth": 16,
            }
            candidates, ownership = pipeline.apply_candidate_mode(profile, route_masks, root)
            self.assertEqual((200, 0, 210, 255), candidates[0].getpixel((0, 0)))
            self.assertEqual((10, 0, 20, 255), candidates[0].getpixel((128, 128)))
            for record in ownership:
                self.assertEqual(65536, record["routeDerivedPixels"] + record["historicalPixels"])
                self.assertEqual(0, record["fallbackPixels"])

    def test_model_call_contract_is_enforced(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "source.png"
            prompt = root / "prompt.txt"
            raw = root / "Raw.png"
            model_call = root / "model-call.json"
            Image.new("RGBA", (4, 4), (255, 255, 255, 255)).save(source)
            Image.new("RGBA", (4, 4), (1, 2, 3, 255)).save(raw)
            prompt.write_text("contract\n", encoding="utf-8")
            model_call.write_text(
                json.dumps(
                    {
                        "tool": "fixture-model",
                        "toolMode": "test",
                        "useCase": "contract-test",
                        "inputPath": str(source),
                        "inputSha256": pipeline.sha256_file(source),
                        "promptPath": str(prompt),
                        "promptSha256AtCall": pipeline.sha256_file(prompt),
                        "callCountExecuted": 1,
                        "retryCount": 0,
                        "fallbackUsed": False,
                        "rawPath": str(raw),
                        "rawSha256": pipeline.sha256_file(raw),
                    }
                ),
                encoding="utf-8",
            )
            result = pipeline.validate_model_call(
                model_call, root / "run", source, prompt, raw, False, False
            )
            self.assertTrue(result["enforced"])
            self.assertEqual(1, result["modelCallCount"])

    def test_single_stress_atlas_preserves_all_four_scenarios(self):
        runtime_masks = {
            mask: Image.new("RGBA", (32, 32), (mask, 255 - mask, mask * 7, 255))
            for mask in range(16)
        }
        atlas, panels = pipeline.build_stress_atlas(runtime_masks)
        self.assertEqual((1024, 1024), atlas.size)
        self.assertEqual(
            [
                "pureLandform",
                "landformWithCentralBaseHole",
                "baseWithCentralLandformIsland",
                "diagonalMixed",
            ],
            [record["id"] for record in panels],
        )
        for index, ((_scenario_id, field), record) in enumerate(
            zip(pipeline.stress_vertex_functions(), panels)
        ):
            x, y, width, height = pipeline.stress_panel_rect(index)
            self.assertEqual([x, y, width, height], record["rect"])
            self.assertEqual(
                pipeline.stress_map(runtime_masks, field).tobytes(),
                atlas.crop((x, y, x + width, y + height)).tobytes(),
            )

        seen_masks = set()
        for _scenario_id, field in pipeline.stress_vertex_functions():
            for y in range(pipeline.STRESS_TILE_COUNT):
                for x in range(pipeline.STRESS_TILE_COUNT):
                    seen_masks.add(
                        (pipeline.NW if field(x, y) else 0)
                        | (pipeline.NE if field(x + 1, y) else 0)
                        | (pipeline.SE if field(x + 1, y + 1) else 0)
                        | (pipeline.SW if field(x, y + 1) else 0)
                    )
        self.assertEqual(set(range(16)), seen_masks)

    def test_stress_atlas_validation_detects_corrupted_panel(self):
        runtime_masks = {
            mask: Image.new("RGBA", (32, 32), (mask, mask, mask, 255)) for mask in range(16)
        }
        atlas, panels = pipeline.build_stress_atlas(runtime_masks)
        record = pipeline.stress_atlas_record(
            atlas,
            Path("run") / pipeline.STRESS_ATLAS_RELATIVE_PATH,
            Path("run"),
            panels,
        )
        self.assertEqual([], pipeline.stress_atlas_failures(atlas, record, runtime_masks))
        corrupted = atlas.copy()
        corrupted.putpixel((750, 750), (255, 0, 255, 255))
        failures = pipeline.stress_atlas_failures(corrupted, record, runtime_masks)
        self.assertIn("stress-atlas-rebuild", failures)
        self.assertIn("stress-panel-rebuild:diagonalMixed", failures)


if __name__ == "__main__":
    unittest.main()
