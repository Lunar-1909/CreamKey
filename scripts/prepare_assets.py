import argparse
import io
import shutil
import sys
import zipfile
from pathlib import Path

import numpy as np
import soundfile as sf


KEYBOARD_PREFIX = "assets/creamykeys/sounds/keyboards/"
MOUSE_SOUND_MAP = {
    "mouse_left.wav": "assets/creamykeys/bundled_keyboards/hit7/corsair_lmb_down.ogg",
    "mouse_right.wav": "assets/creamykeys/bundled_keyboards/hit7/corsair_lmb_down.ogg",
    "mouse_middle.wav": "assets/creamykeys/bundled_keyboards/hit8/corsair_mmb_down.ogg",
    "mouse_x1.wav": "assets/creamykeys/bundled_keyboards/hit7/corsair_lmb_down.ogg",
    "mouse_x2.wav": "assets/creamykeys/bundled_keyboards/hit7/corsair_lmb_down.ogg",
}


def convert_entry(zip_file, entry_name, output_path, skip_existing=False):
    if skip_existing and output_path.exists() and output_path.stat().st_size > 0:
        return False

    raw = zip_file.read(entry_name)
    data, sample_rate = sf.read(io.BytesIO(raw), dtype="float32", always_2d=False)

    if data.ndim == 2:
        data = data.mean(axis=1)

    data = np.clip(data, -1.0, 1.0)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    sf.write(str(output_path), data, sample_rate, subtype="PCM_16", format="WAV")
    return True


def prepare_assets(jar_path, output_root, clean):
    jar_path = Path(jar_path)
    output_root = Path(output_root)
    keyboard_root = output_root / "keyboards"
    mouse_root = output_root / "mouse"

    if not jar_path.exists():
        raise FileNotFoundError(jar_path)

    skip_existing = False
    if clean and keyboard_root.exists():
        try:
            shutil.rmtree(str(keyboard_root))
        except PermissionError as exc:
            print("Could not clean existing keyboard assets, reusing them: {0}".format(exc))
            skip_existing = True
    if clean and mouse_root.exists():
        try:
            shutil.rmtree(str(mouse_root))
        except PermissionError as exc:
            print("Could not clean existing mouse assets, reusing them: {0}".format(exc))
            skip_existing = True

    converted = 0
    converted_mouse = 0
    presets = set()

    with zipfile.ZipFile(str(jar_path), "r") as zip_file:
        for name in zip_file.namelist():
            if not name.startswith(KEYBOARD_PREFIX) or not name.endswith(".ogg"):
                continue

            relative = name[len(KEYBOARD_PREFIX) :]
            parts = relative.split("/")
            if len(parts) != 2:
                continue

            preset, file_name = parts
            output_name = Path(file_name).with_suffix(".wav").name
            output_path = keyboard_root / preset / output_name
            if convert_entry(zip_file, name, output_path, skip_existing=skip_existing):
                converted += 1
            presets.add(preset)

        for output_name, entry_name in MOUSE_SOUND_MAP.items():
            if entry_name not in zip_file.namelist():
                continue
            if convert_entry(zip_file, entry_name, mouse_root / output_name, skip_existing=skip_existing):
                converted_mouse += 1

    available_keyboard = list(keyboard_root.glob("*/*.wav"))
    if converted == 0 and len(available_keyboard) == 0:
        raise RuntimeError("No CreamyKeys keyboard .ogg assets were found.")

    print("Converted {0} sounds across {1} presets.".format(converted, len(presets)))
    print("Available keyboard wav files: {0}".format(len(available_keyboard)))
    print("Converted {0} mouse sounds.".format(converted_mouse))
    for preset in sorted(presets):
        count = len(list((keyboard_root / preset).glob("*.wav")))
        print("  {0}: {1} wav files".format(preset, count))


def main(argv):
    parser = argparse.ArgumentParser(description="Extract CreamyKeys .ogg assets to WAV.")
    parser.add_argument("--jar", required=True, help="Path to CreamyKeys jar.")
    parser.add_argument("--out", required=True, help="Output assets directory.")
    parser.add_argument("--no-clean", action="store_true", help="Keep existing assets.")
    args = parser.parse_args(argv)

    prepare_assets(args.jar, args.out, clean=not args.no_clean)


if __name__ == "__main__":
    main(sys.argv[1:])
