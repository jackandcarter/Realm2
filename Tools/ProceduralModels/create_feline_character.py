#!/usr/bin/env python3
"""Generate a stylized feline humanoid OBJ mesh for Unity."""
from __future__ import annotations

import math
from dataclasses import dataclass
from pathlib import Path
from typing import List, Sequence, Tuple

Vec3 = Tuple[float, float, float]
Face = Tuple[int, int, int]


@dataclass
class Mesh:
    vertices: List[Vec3]
    faces: List[Tuple[int, int, int]]

    def add(self, other: "Mesh") -> None:
        offset = len(self.vertices)
        self.vertices.extend(other.vertices)
        self.faces.extend(tuple(idx + offset for idx in face) for face in other.faces)


def lathe(profile: Sequence[Tuple[float, float]], radial_segments: int, offset: Vec3 = (0.0, 0.0, 0.0)) -> Mesh:
    vertices: List[Vec3] = []
    faces: List[Face] = []
    for i in range(radial_segments):
        theta = 2.0 * math.pi * i / radial_segments
        next_theta = 2.0 * math.pi * (i + 1) / radial_segments
        cos_t, sin_t = math.cos(theta), math.sin(theta)
        cos_nt, sin_nt = math.cos(next_theta), math.sin(next_theta)
        for radius, y in profile:
            vertices.append((radius * cos_t + offset[0], y + offset[1], radius * sin_t + offset[2]))
        if i < radial_segments:
            for j in range(len(profile) - 1):
                a = i * len(profile) + j
                b = a + len(profile)
                c = b + 1
                d = a + 1
                faces.append((a, b, c))
                faces.append((a, c, d))
    # Close seam
    for j in range(len(profile) - 1):
        a = (radial_segments - 1) * len(profile) + j
        b = j
        c = b + 1
        d = a + 1
        faces.append((a, b, c))
        faces.append((a, c, d))
    return Mesh(vertices, faces)


def cylinder(radius: float, height: float, radial_segments: int, offset: Vec3 = (0.0, 0.0, 0.0)) -> Mesh:
    profile = [(radius, -height / 2.0), (radius, height / 2.0)]
    return lathe(profile, radial_segments, offset)


def cone(base_radius: float, height: float, radial_segments: int, offset: Vec3 = (0.0, 0.0, 0.0)) -> Mesh:
    profile = [(base_radius, 0.0), (0.001, height)]
    return lathe(profile, radial_segments, offset)


def pyramid(base: float, depth: float, height: float, offset: Vec3) -> Mesh:
    x, y, z = offset
    half_x = base / 2.0
    half_z = depth / 2.0
    vertices = [
        (x - half_x, y, z - half_z),
        (x + half_x, y, z - half_z),
        (x + half_x, y, z + half_z),
        (x - half_x, y, z + half_z),
        (x, y + height, z),
    ]
    faces = [
        (0, 1, 2),
        (0, 2, 3),
        (0, 1, 4),
        (1, 2, 4),
        (2, 3, 4),
        (3, 0, 4),
    ]
    return Mesh(vertices, faces)


def translate(mesh: Mesh, offset: Vec3) -> Mesh:
    ox, oy, oz = offset
    return Mesh([(x + ox, y + oy, z + oz) for x, y, z in mesh.vertices], mesh.faces.copy())


def mirror(mesh: Mesh, axis: str = "x") -> Mesh:
    if axis != "x":
        raise NotImplementedError
    return Mesh([(-x, y, z) for x, y, z in mesh.vertices], mesh.faces.copy())


def build_character() -> Mesh:
    mesh = Mesh([], [])

    torso_profile = [
        (0.02, -0.9),
        (0.28, -0.9),
        (0.35, -0.3),
        (0.37, 0.3),
        (0.3, 0.75),
        (0.18, 1.05),
        (0.05, 1.3),
    ]
    mesh.add(lathe(torso_profile, radial_segments=28))

    head_profile = [
        (0.001, -0.15),
        (0.2, -0.12),
        (0.28, 0.05),
        (0.23, 0.22),
        (0.15, 0.33),
        (0.001, 0.38),
    ]
    mesh.add(translate(lathe(head_profile, 24), (0.0, 1.35, 0.0)))

    snout_profile = [
        (0.05, -0.05),
        (0.08, 0.02),
        (0.03, 0.08),
    ]
    mesh.add(translate(lathe(snout_profile, 16), (0.0, 1.47, 0.15)))

    ear = pyramid(0.12, 0.12, 0.22, (0.15, 1.6, 0.0))
    mesh.add(ear)
    mesh.add(mirror(ear))

    arm = translate(cylinder(0.1, 0.9, 20), (0.45, 0.55, 0.0))
    mesh.add(arm)
    mesh.add(mirror(arm))

    forearm = translate(cylinder(0.08, 0.85, 20), (0.65, 0.0, 0.0))
    mesh.add(forearm)
    mesh.add(mirror(forearm))

    hand = translate(cone(0.11, 0.18, 16), (0.83, -0.35, 0.0))
    mesh.add(hand)
    mesh.add(mirror(hand))

    thigh = translate(cylinder(0.17, 1.0, 20), (0.22, -0.6, 0.0))
    mesh.add(thigh)
    mesh.add(mirror(thigh))

    shin = translate(cylinder(0.13, 0.9, 20), (0.15, -1.15, 0.0))
    mesh.add(shin)
    mesh.add(mirror(shin))

    foot = translate(cone(0.15, 0.25, 20), (0.2, -1.6, 0.15))
    mesh.add(foot)
    mesh.add(mirror(foot))

    tail_profile = [
        (0.07, 0.0),
        (0.05, 0.4),
        (0.04, 0.8),
        (0.03, 1.1),
        (0.02, 1.4),
        (0.01, 1.8),
        (0.005, 2.1),
    ]
    tail = translate(lathe(tail_profile, 18), (0.0, 0.2, -0.28))
    mesh.add(tail)

    return mesh


def write_obj(path: Path, mesh: Mesh) -> None:
    with path.open("w", encoding="utf-8") as fh:
        fh.write("# Procedurally generated feline humanoid\n")
        for x, y, z in mesh.vertices:
            fh.write(f"v {x:.6f} {y:.6f} {z:.6f}\n")
        for face in mesh.faces:
            # OBJ indices are 1-based
            fh.write(f"f {face[0] + 1} {face[1] + 1} {face[2] + 1}\n")


def main() -> None:
    mesh = build_character()
    output_path = Path(__file__).parents[2] / "Assets" / "Resources" / "Models"
    output_path.mkdir(parents=True, exist_ok=True)
    obj_path = output_path / "FelineHumanoid.obj"
    write_obj(obj_path, mesh)
    print(f"Saved {len(mesh.vertices)} vertices and {len(mesh.faces)} faces to {obj_path}")


if __name__ == "__main__":
    main()
