import json
from compact_json_encoder import CompactJSONEncoder

info: dict
with open("info.json") as f:
    info = json.load(f)

folder = "ex2"

id: str
for id in info:
    if not id.startswith(folder): continue

    participant: int = int(id[7])
    performance: int = int(id[12])

    distances: list[str]
    with open(f"{folder}/edit_distance/{participant}/{performance}.txt") as f:
        distances = [int(x) for x in  f.readlines()]
    
    info[id]["edit_distances"] = distances


with open("info.json", "w") as f:
    json.dump(info, f, indent=4, cls=CompactJSONEncoder)