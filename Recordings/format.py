import json
from util import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)

with open("info.json", "w") as f:
    json.dump(info, f, indent=4, cls=CompactJSONEncoder)