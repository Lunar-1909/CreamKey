import math
import struct
from pathlib import Path


SIZES = (16, 24, 32, 48, 64, 128, 256)


def clamp(value, low, high):
    return max(low, min(high, value))


def lerp(a, b, t):
    return int(round(a + (b - a) * t))


def rounded_rect_alpha(x, y, w, h, radius):
    px = x + 0.5
    py = y + 0.5
    qx = abs(px - w / 2.0) - (w / 2.0 - radius)
    qy = abs(py - h / 2.0) - (h / 2.0 - radius)
    ox = max(qx, 0.0)
    oy = max(qy, 0.0)
    distance = math.sqrt(ox * ox + oy * oy) + min(max(qx, qy), 0.0) - radius
    return clamp(0.5 - distance, 0.0, 1.0)


def blend(dst, src):
    sr, sg, sb, sa = src
    dr, dg, db, da = dst
    sa /= 255.0
    da /= 255.0
    out_a = sa + da * (1.0 - sa)
    if out_a <= 0:
        return (0, 0, 0, 0)
    out_r = (sr * sa + dr * da * (1.0 - sa)) / out_a
    out_g = (sg * sa + dg * da * (1.0 - sa)) / out_a
    out_b = (sb * sa + db * da * (1.0 - sa)) / out_a
    return (int(out_r), int(out_g), int(out_b), int(out_a * 255))


def fill_rounded_rect(img, x0, y0, x1, y1, radius, color):
    size = len(img)
    w = x1 - x0
    h = y1 - y0
    for y in range(max(0, y0), min(size, y1)):
        for x in range(max(0, x0), min(size, x1)):
            a = rounded_rect_alpha(x - x0, y - y0, w, h, radius)
            if a > 0:
                r, g, b, alpha = color
                img[y][x] = blend(img[y][x], (r, g, b, int(alpha * a)))


def stroke_arc(img, cx, cy, radius, width, start_angle, end_angle, color):
    size = len(img)
    for y in range(size):
        for x in range(size):
            dx = x + 0.5 - cx
            dy = y + 0.5 - cy
            dist = math.sqrt(dx * dx + dy * dy)
            angle = math.atan2(dy, dx)
            while angle < start_angle:
                angle += math.tau
            inside_angle = start_angle <= angle <= end_angle
            edge = abs(dist - radius) - width / 2.0
            a = clamp(0.75 - edge, 0.0, 1.0) if inside_angle else 0.0
            if a > 0:
                r, g, b, alpha = color
                img[y][x] = blend(img[y][x], (r, g, b, int(alpha * a)))


def render(size):
    img = [[(0, 0, 0, 0) for _ in range(size)] for _ in range(size)]

    for y in range(size):
        for x in range(size):
            t = (x + y) / max(1, size * 2 - 2)
            bg = (lerp(22, 28, t), lerp(104, 67, t), lerp(117, 89, t), 255)
            img[y][x] = bg

    pad = max(1, round(size * 0.08))
    fill_rounded_rect(img, pad, pad, size - pad, size - pad, max(3, round(size * 0.20)),
                      (18, 87, 99, 255))

    key_x0 = round(size * 0.18)
    key_y0 = round(size * 0.34)
    key_x1 = round(size * 0.70)
    key_y1 = round(size * 0.73)
    shadow = max(1, round(size * 0.04))
    fill_rounded_rect(img, key_x0 + shadow, key_y0 + shadow, key_x1 + shadow, key_y1 + shadow,
                      max(2, round(size * 0.08)), (4, 31, 36, 90))
    fill_rounded_rect(img, key_x0, key_y0, key_x1, key_y1, max(2, round(size * 0.08)),
                      (245, 238, 222, 255))
    fill_rounded_rect(img, key_x0 + round(size * 0.06), key_y0 + round(size * 0.08),
                      key_x1 - round(size * 0.06), key_y1 - round(size * 0.10),
                      max(1, round(size * 0.04)), (230, 219, 198, 180))

    accent_w = max(2, round(size * 0.06))
    fill_rounded_rect(img, key_x0 + round(size * 0.10), key_y0 + round(size * 0.12),
                      key_x0 + round(size * 0.10) + accent_w, key_y1 - round(size * 0.14),
                      max(1, round(size * 0.02)), (212, 137, 72, 220))

    cx = size * 0.70
    cy = size * 0.50
    for i, radius in enumerate((0.15, 0.24, 0.33)):
        stroke_arc(img, cx, cy, size * radius, max(1.0, size * 0.025),
                   -0.82, 0.82, (246, 196, 94, 210 - i * 35))

    return img


def make_dib(img):
    size = len(img)
    width = size
    height = size * 2
    row_bytes = width * 4
    and_stride = ((width + 31) // 32) * 4
    header = struct.pack(
        "<IIIHHIIIIII",
        40,
        width,
        height,
        1,
        32,
        0,
        row_bytes * size + and_stride * size,
        0,
        0,
        0,
        0,
    )

    pixels = bytearray()
    for y in range(size - 1, -1, -1):
        for x in range(size):
            r, g, b, a = img[y][x]
            pixels.extend((b, g, r, a))

    mask = bytes(and_stride * size)
    return header + bytes(pixels) + mask


def write_ico(path):
    images = [(size, make_dib(render(size))) for size in SIZES]
    directory_size = 6 + len(images) * 16
    offset = directory_size

    out = bytearray()
    out.extend(struct.pack("<HHH", 0, 1, len(images)))
    entries = bytearray()
    payload = bytearray()

    for size, data in images:
        width_byte = 0 if size == 256 else size
        height_byte = 0 if size == 256 else size
        entries.extend(struct.pack("<BBBBHHII", width_byte, height_byte, 0, 0, 1, 32, len(data), offset))
        payload.extend(data)
        offset += len(data)

    out.extend(entries)
    out.extend(payload)
    Path(path).write_bytes(out)


if __name__ == "__main__":
    write_ico(Path(__file__).resolve().parents[1] / "app.ico")
