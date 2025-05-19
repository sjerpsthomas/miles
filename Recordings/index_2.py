import json
from util import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)


for performance in info:
    if str(performance).startswith("ex1"):
        info[performance]["scores"] = { -1: info[performance]["scores"] }



with open("info_2.json", "w") as f:
    asdf = json.dumps(info, indent=4, cls=CompactJSONEncoder)

    f.write(asdf)