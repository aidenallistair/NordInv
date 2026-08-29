#!/usr/bin/env python3
"""
Генератор сцен Nord Invasion для Bannerlord (пункт плана: 4 карты mp_ni_*).

Формат .xscene сверен с реальными сценами Bannerlord:
- корень <scene name=... version="2">
- entry points = сущности mp_spawnpoint (prefab из vanilla)
- пропсы = vanilla prefab'ы (существуют в игре, не требуют бинарных мешей мода)

ВАЖНО: .xscene - это только сущности. Террейн (terrain.bin, flora.bin,
ShaderCache) - бинарные данные, которые генерирует Bannerlord Scene Editor.
После генерации прогони tools/prepare_scenes.py (копирует террейн из vanilla
сцены) либо открой сцену в Scene Editor и сохрани.

Запуск:
    python3 tools/gen_ni_scenes.py
Результат:
    BannerlordModule/Modules/NordInvasion/ModuleData/Scenes/mp_ni_*/
"""
import math
import os

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCENES_ROOT = os.path.join(
    REPO_ROOT, "BannerlordModule", "Modules", "NordInvasion", "ModuleData", "Scenes"
)

MAP_SIZE = 200.0  # квадрат 200x200 (как у vanilla mp-сцен)

ENV_PROPERTIES = """\
\t<environment_properties>
\t\t<property name="nav_mesh_auto_generated_" value="false"/>
\t\t<property name="physics_world_min" value="0.000, 0.000"/>
\t\t<property name="physics_world_max_" value="{size:.3f}, {size:.3f}"/>
\t\t<property name="north_angle" value="0.000"/>
\t\t<property name="is_indoors" value="false"/>
\t\t<property name="disable_flora_billboards" value="false"/>
\t\t<property name="ground_color" value="0.000, 0.000, 0.000"/>
\t\t<property name="prt_multiplier" value="1.000"/>
\t\t<property name="prt_point_light_multiplier" value="1.000"/>
\t\t<property name="fog_start_distance" value="50.000"/>
\t\t<property name="prt_color_contrast" value="1.000"/>
\t\t<property name="scene_scale_multiplier" value="1.000"/>
\t\t<property name="prt_intensity_contrast" value="1.000"/>
\t\t<property name="prt_fallback_probe_index" value="-1, -1, -1"/>
\t\t<property name="enforced_color_grade" value=""/>
\t\t<atmosphere_properties>
\t\t\t<property name="atmosphere_name" value="scene_atmosphere"/>
\t\t\t<property name="wind" value="1.000, 0.000"/>
\t\t\t<property name="scene_fog_falloff_offset" value="0.000"/>
\t\t</atmosphere_properties>
\t\t<water_properties version="1">
\t\t\t<property name="water_level" value="-100.000"/>
\t\t\t<property name="water_strength" value="5.000"/>
\t\t\t<property name="water_wind_dependency" value="1.000"/>
\t\t\t<property name="water_material" value="water_default"/>
\t\t\t<property name="water_shallow_color" value="1.000, 1.000, 1.000"/>
\t\t\t<property name="water_deep_color" value="1.000, 1.000, 1.000"/>
\t\t\t<property name="water_exists" value="false"/>
\t\t\t<property name="place_water_probe" value="true"/>
\t\t</water_properties>
\t\t<out_of_bounds_effects_properties>
\t\t\t<property name="vignette_color" value="0.000, 0.500, 1.000"/>
\t\t\t<property name="vignette_alpha" value="0.000"/>
\t\t</out_of_bounds_effects_properties>
\t</environment_properties>"""

# atmosphere.xml - из vanilla-шаблона (JacobPersi/Bannerlord-Custom-Mission-Template)
ATMOSPHERE = """\
<atmosphere>
\t<values>
\t\t<value name="name" value="scene_atmosphere"/>
\t\t<value name="cloud_amount" value="0.100"/>
\t\t<value name="middle_gray" value="0.133"/>
\t\t<value name="temperature" value="40.000"/>
\t\t<value name="humidity" value="0.000"/>
\t\t<value name="color_grade_name" value="cg_50c_5b"/>
\t\t<value name="time_of_day" value="12.000"/>
\t\t<value name="fall_density" value="0.000"/>
\t\t<value name="snow_density" value="0.000"/>
\t\t<value name="is_indoor" value="false"/>
\t\t<value name="global_envmap_multiplier" value="1.000"/>
\t\t<value name="global_envmap_color_factor" value="0.885, 0.923, 1.000"/>
\t\t<value name="prt_multiplier" value="1.000"/>
\t\t<value name="do_not_render_envmap" value="false"/>
\t\t<value name="skybox_background_texture_name" value="semi_cloudy_2"/>
\t\t<value name="skybox_sun_texture_name" value="sun_core2"/>
\t\t<value name="lens_flare_dirt_texture_name" value="lens_dirt"/>
\t\t<value name="lens_flare_star_texture_name" value="lens_star"/>
\t\t<value name="skybox_panaroma_type" value="0"/>
\t\t<value name="season" value="summer"/>
\t</values>
\t<global_ambient fog_ambient_color="1.000, 0.642, 0.402"/>
\t<fog>
\t\t<value name="fog_density" value="1.000"/>
\t\t<value name="fog_falloff" value="0.360"/>
\t\t<value name="fog_color" value="0.557, 0.762, 0.890"/>
\t\t<value name="cloud_shadow_color" value="0.250, 0.250, 0.400"/>
\t\t<value name="mie_scatter_particle_size" value="5.619"/>
\t\t<value name="skybox_distance" value="6000.000"/>
\t\t<value name="scene_scale" value="1.000"/>
\t\t<value name="skybox_height" value="7500.000"/>
\t\t<value name="horizon_distance" value="7500.000"/>
\t\t<value name="fog_falloff_offset" value="0.000"/>
\t\t<value name="fog_falloff_minfog" value="0.000"/>
\t\t<value name="fog_start_distance" value="0.000"/>
\t\t<value name="fog_scatter" value="0.200"/>
\t\t<value name="scatter_strength" value="5.987"/>
\t</fog>
\t<cloud_shadow>
\t\t<value name="cloud_shadow_amount" value="0.000"/>
\t\t<value name="cloud_shadow_contrast" value="3.000"/>
\t\t<value name="cloud_shadow_begin_height" value="0.000"/>
\t\t<value name="cloud_shadow_end_height" value="1000.000"/>
\t\t<value name="cloud_shadow_scale" value="0.200"/>
\t\t<value name="cloud_shadow_speed" value="0.200"/>
\t</cloud_shadow>
\t<sun>
\t\t<value name="skybox_rotation" value="0.000"/>
\t\t<value name="sun_altitude" value="13.826"/>
\t\t<value name="sun_angle" value="20.000"/>
\t\t<value name="shadow_opacity" value="1.000"/>
\t\t<value name="minimum_ambient" value="0.000"/>
\t\t<value name="sun_rotation" value="90.000"/>
\t\t<value name="sun_intesity" value="1026.219"/>
\t\t<value name="sky_brightness" value="120.165"/>
\t\t<value name="cloud_brightness" value="0.700"/>
\t\t<value name="sun_size" value="0.124"/>
\t\t<value name="sky_noise_amount" value="0.344"/>
\t\t<value name="sunshafts_strength" value="1.000"/>
\t\t<value name="sun_color" value="1.000, 0.582, 0.313"/>
\t\t<value name="rayleigh_constant" value="0.390"/>
\t</sun>
\t<cubemap_texture env_map_name="default_cubemap"/>
\t<flags/>
</atmosphere>
"""


def yaw_to(cx: float, cy: float, tx: float, ty: float) -> float:
    """Yaw (рад) от (cx,cy) к (tx,ty) в системе Bannerlord (z - up)."""
    return math.atan2(tx - cx, ty - cy)


def spawnpoint(x: float, y: float, yaw: float) -> str:
    return (
        '\t\t<game_entity name="mp_spawnpoint" old_prefab_name="mp_spawnpoint">\n'
        "\t\t\t<visibility_masks>\n"
        "\t\t\t\t<visibility_mask name=\"visible_only_when_editing\" value=\"true\"/>\n"
        "\t\t\t</visibility_masks>\n"
        "\t\t\t<tags>\n"
        "\t\t\t\t<tag name=\"spawnpoint\"/>\n"
        "\t\t\t</tags>\n"
        f"\t\t\t<transform position=\"{x:.3f}, {y:.3f}, 0.000\" rotation_euler=\"0.000, 0.000, {yaw:.3f}\" scale=\"1.637, 1.637, 1.637\"/>\n"
        "\t\t\t<components>\n"
        "\t\t\t\t<meta_mesh_component name=\"arrow_new_icon\"/>\n"
        "\t\t\t</components>\n"
        "\t\t</game_entity>\n"
    )


def prop(name: str, x: float, y: float, yaw: float = 0.0, scale: float = 1.0) -> str:
    s = f" scale=\"{scale:.3f}, {scale:.3f}, {scale:.3f}\"" if scale != 1.0 else ""
    return (
        f'\t\t<game_entity name="{name}" old_prefab_name="{name}">\n'
        f"\t\t\t<transform position=\"{x:.3f}, {y:.3f}, 0.000\" rotation_euler=\"0.000, 0.000, {yaw:.3f}\"{s}/>\n"
        "\t\t</game_entity>\n"
    )


def borders() -> str:
    m = MAP_SIZE
    half = m * 0.08
    out = []
    # 8 border_soft по краям (как в vanilla mp-сценах)
    for (x, y, a) in [
        (half, half, 0.0), (m / 2, half, math.pi / 4), (m - half, half, math.pi / 2),
        (half, m / 2, -math.pi / 4), (m - half, m / 2, math.pi * 3 / 4),
        (half, m - half, -math.pi / 2), (m / 2, m - half, -math.pi * 3 / 4),
        (m - half, m - half, -math.pi),
    ]:
        out.append(prop("border_soft", x, y, a))
    return "".join(out)


def entry_points(fort_x: float, fort_y: float) -> str:
    """
    0-31: игроки (в форте, лицом наружу)
    32-63: норды (кольцо вокруг форта, лицом в форт)
    64: босс (далеко за кольцом)
    """
    out = []
    # Игроки: 8x4 сетка в форте
    for i in range(32):
        row, col = divmod(i, 8)
        x = fort_x - 9 + col * 3
        y = fort_y - 9 + row * 3
        out.append(spawnpoint(x, y, yaw_to(x, y, fort_x + 40, fort_y)))
    # Норды: кольцо радиус 55-95, углы -100..100 (восточная дуга)
    for i in range(32):
        ang = math.radians(-100 + i * (200.0 / 31))
        r = 55 + (i % 4) * 12
        x = fort_x + r * math.sin(ang)
        y = fort_y + r * math.cos(ang)
        out.append(spawnpoint(x, y, yaw_to(x, y, fort_x, fort_y)))
    # Босс
    out.append(spawnpoint(fort_x + 130, fort_y, math.pi))
    return "".join(out)


def scene(name: str, entities: str, used_props: list) -> str:
    return (
        '<?xml version="1.0"?>\n'
        f'<scene name="{name}" version="2">\n'
        f"\t<flora_bounding_rect min=\"0.000, 0.000\" max=\"{MAP_SIZE:.3f}, {MAP_SIZE:.3f}\"/>\n"
        "\t<levels>\n"
        "\t\t<level name=\"base\" mask=\"1\"/>\n"
        "\t</levels>\n"
        + ENV_PROPERTIES.format(size=MAP_SIZE) + "\n"
        "\t<entities>\n"
        + entities
        + "\t</entities>\n"
        "</scene>\n"
    )


def references(used_props: list) -> str:
    # references.txt - список ассетов (mesh-подсказки для загрузчика).
    # prepare_scenes.py заменит его vanilla-версией из исходной сцены (надежнее).
    lines = ["mesh arrow_new_icon"]
    for p in sorted(set(used_props)):
        lines.append(f"mesh {p}")
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------------------
# Раскладки 4 карт (форты на западе, норды на востоке)
# ---------------------------------------------------------------------------

def map_bridge() -> dict:
    fx, fy = 35.0, 100.0
    props, used = [], ["border_soft", "mp_spawnpoint"]
    # Мост: платформа через "реку" x=60..75
    for i in range(5):
        props.append(prop("wooden_platform_2_a", 62 + i * 4.0, 100.0, 0.0, 1.5))
    used += ["wooden_platform_2_a"]
    # Ворота форта (юг/север моста)
    props.append(prop("battania_castle_keep_a_l3_door_b", 55.0, 88.0, 0.0))
    props.append(prop("battania_castle_keep_a_l3_door_b", 55.0, 112.0, math.pi))
    used.append("battania_castle_keep_a_l3_door_b")
    # Стены форта
    for i in range(7):
        props.append(prop("empire_garden_wall_a1", 20.0 + i * 5.0, 78.0, 0.0))
        props.append(prop("empire_garden_wall_a1", 20.0 + i * 5.0, 122.0, 0.0))
    used.append("empire_garden_wall_a1")
    # Фасции вдоль моста
    for i in range(6):
        props.append(prop("fence_empire_a", 60.0 + i * 4.0, 94.0, math.pi / 2))
        props.append(prop("fence_empire_a", 60.0 + i * 4.0, 106.0, math.pi / 2))
    used.append("fence_empire_a")
    # Факелы в форте
    for (tx, ty) in [(25, 85), (45, 85), (25, 115), (45, 115), (58, 100)]:
        props.append(prop("torch_a_wm", tx, ty))
    used.append("torch_a_wm")
    # Казна + костер
    props.append(prop("vlandia_chest_c", 30.0, 100.0))
    props.append(prop("fire_stones_bonfire", 38.0, 100.0))
    used += ["vlandia_chest_c", "fire_stones_bonfire"]
    # Пороховые бочки (для взрывов)
    for (bx, by) in [(70, 95), (72, 105), (90, 100)]:
        props.append(prop("bd_barrel_a", bx, by))
    used.append("bd_barrel_a")
    return dict(fort=(fx, fy), props="".join(props), used=used)


def map_town() -> dict:
    fx, fy = 35.0, 100.0
    props, used = [], ["border_soft", "mp_spawnpoint"]
    # Улицы из домиков (узкие)
    house_spots = [
        (25, 80), (40, 80), (25, 92), (40, 92), (25, 108), (40, 108), (25, 120), (40, 120),
        (55, 85), (55, 100), (55, 115),
    ]
    for (hx, hy) in house_spots:
        props.append(prop("sturgia_house_a_stair", hx, hy, math.pi / 2))
    used.append("sturgia_house_a_stair")
    # Палатки
    for (tx, ty) in [(58, 90), (58, 110), (65, 100)]:
        props.append(prop("village_tent_e", tx, ty))
    used.append("village_tent_e")
    # Заборы-улицы
    for i in range(6):
        props.append(prop("fence_empire_a", 20.0 + i * 5.0, 97.0, 0.0))
        props.append(prop("fence_empire_a", 20.0 + i * 5.0, 103.0, 0.0))
    used.append("fence_empire_a")
    for (tx, ty) in [(22, 85), (45, 85), (22, 115), (45, 115), (55, 95), (55, 105)]:
        props.append(prop("torch_a_wm", tx, ty))
    used.append("torch_a_wm")
    props.append(prop("vlandia_chest_c", 30.0, 100.0))
    props.append(prop("fire_stones_bonfire", 50.0, 100.0))
    props.append(prop("bd_wood_heap_a", 47.0, 88.0))
    props.append(prop("bd_barrel_a", 62.0, 100.0))
    used += ["vlandia_chest_c", "fire_stones_bonfire", "bd_wood_heap_a", "bd_barrel_a"]
    return dict(fort=(fx, fy), props="".join(props), used=used)


def map_castle() -> dict:
    fx, fy = 35.0, 100.0
    props, used = [], ["border_soft", "mp_spawnpoint"]
    # Бастион: стена с воротами + 2 башни
    for i in range(9):
        y = 70.0 + i * 8.0
        if y > 92 and y < 108:
            continue  # проем ворот
        props.append(prop("empire_garden_wall_a1", 55.0, y, math.pi / 2))
    used.append("empire_garden_wall_a1")
    props.append(prop("aserai_castle_wall_a_l3", 55.0, 96.0, math.pi / 2))
    props.append(prop("aserai_castle_wall_a_l3", 55.0, 104.0, math.pi / 2))
    used.append("aserai_castle_wall_a_l3")
    for (tx_, ty_) in [(55, 66), (55, 134)]:
        props.append(prop("aserai_castle_tower_round_a_l3", tx_, ty_))
    used.append("aserai_castle_tower_round_a_l3")
    props.append(prop("battania_castle_keep_a_l3_door_b", 55.0, 100.0, math.pi / 2))
    used.append("battania_castle_keep_a_l3_door_b")
    # Внутренний двор
    for (tx, ty) in [(25, 80), (45, 80), (25, 120), (45, 120)]:
        props.append(prop("torch_a_wm", tx, ty))
    used.append("torch_a_wm")
    props.append(prop("vlandia_chest_c", 30.0, 100.0))
    props.append(prop("fire_stones_bonfire", 40.0, 90.0))
    props.append(prop("bd_barrel_a", 48.0, 108.0))
    props.append(prop("bd_barrel_a", 48.0, 92.0))
    used += ["vlandia_chest_c", "fire_stones_bonfire", "bd_barrel_a"]
    # Подход: мост-настил к воротам
    for i in range(3):
        props.append(prop("wooden_platform_2_a", 60.0 + i * 4.0, 100.0, 0.0, 1.5))
    used.append("wooden_platform_2_a")
    return dict(fort=(fx, fy), props="".join(props), used=used)


def map_forest() -> dict:
    fx, fy = 35.0, 100.0
    props, used = [], ["border_soft", "mp_spawnpoint"]
    # Лес: деревья (свои позиции - детерминированно)
    tree_spots = [
        (70, 70), (85, 85), (100, 65), (110, 120), (95, 140), (120, 90),
        (140, 70), (140, 130), (75, 110), (80, 145), (150, 100), (60, 60),
        (60, 140), (105, 105), (125, 60), (125, 140), (155, 75), (155, 125),
    ]
    for i, (tx, ty) in enumerate(tree_spots):
        p = "tree_root_b_pine" if i % 3 else "mushroom_tree_a"
        props.append(prop(p, tx, ty, (i * 0.7) % math.pi))
    used += ["tree_root_b_pine", "mushroom_tree_a"]
    # Камни
    for (rx, ry) in [(75, 95), (95, 80), (115, 110), (85, 125), (130, 100)]:
        props.append(prop("map_rock_groups_a", rx, ry, rx * 0.1))
    used.append("map_rock_groups_a")
    # Лагерь обороны
    for (tx, ty) in [(25, 85), (45, 85), (25, 115), (45, 115)]:
        props.append(prop("torch_a_wm", tx, ty))
    props.append(prop("village_tent_e", 30.0, 95.0))
    props.append(prop("village_tent_e", 30.0, 105.0))
    props.append(prop("fire_stones_bonfire", 38.0, 100.0))
    props.append(prop("bd_wood_heap_a", 28.0, 100.0))
    props.append(prop("bd_barrel_a", 52.0, 98.0))
    props.append(prop("vlandia_chest_c", 25.0, 100.0))
    props.append(prop("fence_empire_a", 50.0, 85.0, math.pi / 2))
    props.append(prop("fence_empire_a", 50.0, 115.0, math.pi / 2))
    used += ["torch_a_wm", "village_tent_e", "fire_stones_bonfire", "bd_wood_heap_a",
             "bd_barrel_a", "vlandia_chest_c", "fence_empire_a"]
    return dict(fort=(fx, fy), props="".join(props), used=used)


MAPS = {
    "mp_ni_bridge_01": ("NI Bridge Fort - Chokepoint - Best for barricades", map_bridge),
    "mp_ni_town_01": ("NI Town - Narrow streets", map_town),
    "mp_ni_castle_01": ("NI Castle - Gate defense", map_castle),
    "mp_ni_forest_01": ("NI Forest - Ambush", map_forest),
}


def main() -> None:
    os.makedirs(SCENES_ROOT, exist_ok=True)
    for scene_id, (title, builder) in MAPS.items():
        layout = builder()
        fx, fy = layout["fort"]
        entities = borders() + layout["props"] + entry_points(fx, fy)
        xscene = scene(scene_id, entities, layout["used"])
        refs = references(layout["used"])
        folder = os.path.join(SCENES_ROOT, scene_id)
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, "scene.xscene"), "w", encoding="utf-8") as f:
            f.write(xscene)
        with open(os.path.join(folder, "atmosphere.xml"), "w", encoding="utf-8") as f:
            f.write(ATMOSPHERE)
        with open(os.path.join(folder, "references.txt"), "w", encoding="utf-8") as f:
            f.write(refs)
        with open(os.path.join(folder, "README.md"), "w", encoding="utf-8") as f:
            f.write(
                f"# {scene_id}\n\n{title}\n\n"
                "Сгенерировано tools/gen_ni_scenes.py. Entry points:\n"
                "- 0-31: игроки (западный форт)\n"
                "- 32-63: норды (кольцо вокруг форта)\n"
                "- 64: босс (восток)\n\n"
                "Перед запуском нужен террейн: прогони tools/prepare_scenes.py\n"
                "(копирует terrain.bin/flora.bin/ShaderCache из vanilla-сцены)\n"
                "или открой сцену в Bannerlord Scene Editor и сохрани.\n"
            )
        print(f"OK  {scene_id}: {xscene.count(chr(10))} строк xscene")
    print(f"\nГотово: {SCENES_ROOT}")
    print("Следующий шаг: python3 tools/prepare_scenes.py (нужна установка Bannerlord)")


if __name__ == "__main__":
    main()
