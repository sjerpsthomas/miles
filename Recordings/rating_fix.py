import json
from compact_json_encoder import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)



id: str
for id in info:
    if not id.startswith("ex1"): continue

    participant_id: int = int(id[7])

    for sid in info[id]["scores"]:
        if int(sid) != -1:
            info[id]["scores"][sid] = [x + 1 for x in info[id]["scores"][sid]]


with open("info.json", "w") as f:
    json.dump(info, f, indent=4, cls=CompactJSONEncoder)