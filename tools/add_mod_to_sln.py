"""Adds mods/<Name>/<Name>.csproj to ScheduleI-Mods.sln with all four configurations.

Usage: python tools/add_mod_to_sln.py <Name>
"""
import pathlib
import re
import sys
import uuid

ROOT = pathlib.Path(__file__).resolve().parents[1]
CONFIGS = ["Il2Cpp", "Mono", "Il2CppDev", "MonoDev"]


def main() -> None:
    name = sys.argv[1]
    sln = ROOT / "ScheduleI-Mods.sln"
    s = sln.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\n", "\r\n")
    if f'"{name}"' in s:
        print(f"{name} already in solution")
        return
    guid = str(uuid.uuid4()).upper()
    folder = re.search(r'"mods", "mods", "\{([0-9A-F-]+)\}"', s).group(1)
    project = (f'Project("{{9A19103F-16F7-4668-BE54-9A1E7A4F7556}}") = "{name}", '
               f'"mods\\{name}\\{name}.csproj", "{{{guid}}}"\r\nEndProject\r\n')
    s = s.replace("Global\r\n", project + "Global\r\n", 1)
    configs = "".join(f"\t\t{{{guid}}}.{c}|Any CPU.ActiveCfg = {c}|Any CPU\r\n"
                      f"\t\t{{{guid}}}.{c}|Any CPU.Build.0 = {c}|Any CPU\r\n" for c in CONFIGS)
    marker = "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n"
    s = s.replace(marker, marker + configs, 1)
    marker = "\tGlobalSection(NestedProjects) = preSolution\r\n"
    s = s.replace(marker, marker + f"\t\t{{{guid}}} = {{{folder}}}\r\n", 1)
    sln.write_text(s, encoding="utf-8", newline="")
    print(f"added {name} ({guid})")


if __name__ == "__main__":
    main()
