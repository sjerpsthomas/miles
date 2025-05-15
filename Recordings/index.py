import json
from util import CompactJSONEncoder

with open("ex_1/info.json") as f:
    info = json.load(f)


all_info = {}


for pupil_index, pupil in enumerate(info["pupils"]):
    for session_index, session in enumerate(pupil["sessions"]):
        for performance_index, performance in enumerate(session["performance"]):
            s = f"ex_1_par{pupil_index}_ses{session_index}_per{performance_index}"

            with open(f"ex_1/edit_distance/{pupil_index + 1}/{session_index + 1}/{performance_index + 1}.txt") as f:
                distances = [int(v) for v in f.readlines()]
            
            performance["distances"] = distances
            performance["path"] = f"ex_1/{pupil_index}/{session_index}/{performance_index}.notes"

            all_info[s] = performance






with open("all_info.json", "w") as f:
    asdf = json.dumps(all_info, indent=4, cls=CompactJSONEncoder)

    f.write(asdf)