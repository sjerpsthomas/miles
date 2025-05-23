import json
from compact_json_encoder import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)

config = {
    0: [
        ['p3s1p1', 'p3s4p1'],
        ['p4s4p2', 'p4s1p2'],
        ['p5s1p3', 'p5s4p3'],
        ['p1s4p1', 'p1s1p1'],
        ['p2s1p2', 'p2s4p2'],
        ['p3s1p3', 'p3s4p3'],
        ['p4s1p1', 'p4s4p1'],
        ['p5s4p2', 'p5s1p2'],
        ['p1s4p3', 'p1s1p3'],
        ['p2s1p1', 'p2s4p1'],
        ['p3s4p2', 'p3s1p2'],
        ['p4s4p3', 'p4s1p3'],
        ['p5s4p1', 'p5s1p1'],
        ['p1s4p2', 'p1s1p2'],
        ['p2s4p3', 'p2s1p3']
    ],
    1: [
        ['p1s4p1', 'p1s1p1'],
        ['p2s1p2', 'p2s4p2'],
        ['p3s4p3', 'p3s1p3'],
        ['p4s4p1', 'p4s1p1'],
        ['p5s4p2', 'p5s1p2'],
        ['p1s1p3', 'p1s4p3'],
        ['p2s4p1', 'p2s1p1'],
        ['p3s1p2', 'p3s4p2'],
        ['p4s1p3', 'p4s4p3'],
        ['p5s1p1', 'p5s4p1'],
        ['p1s4p2', 'p1s1p2'],
        ['p2s1p3', 'p2s4p3'],
        ['p3s1p1', 'p3s4p1'],
        ['p4s4p2', 'p4s1p2'],
        ['p5s4p3', 'p5s1p3']
    ],
    2: [
        ['p5s1p1', 'p5s4p1'],
        ['p1s4p2', 'p1s1p2'],
        ['p2s4p3', 'p2s1p3'],
        ['p3s1p1', 'p3s4p1'],
        ['p4s1p2', 'p4s4p2'],
        ['p5s4p3', 'p5s1p3'],
        ['p1s1p1', 'p1s4p1'],
        ['p2s1p2', 'p2s4p2'],
        ['p3s4p3', 'p3s1p3'],
        ['p4s4p1', 'p4s1p1'],
        ['p5s4p2', 'p5s1p2'],
        ['p1s4p3', 'p1s1p3'],
        ['p2s4p1', 'p2s1p1'],
        ['p3s4p2', 'p3s1p2'],
        ['p4s1p3', 'p4s4p3']
    ]
}

results = {
    0: [1,0,1,0,0,0,0,1,0,0,1,1,0,0,0],
    1: [1,0,1,1,1,0,1,1,0,0,1,1,0,1,0],
    2: [0,1,0,0,0,0,0,1,1,1,0,0,0,0,1],
}

def new_id(old_id: str) -> str:
    participant_id = int(old_id[1]) - 1
    session_id = int(old_id[3]) - 1
    performance_id = int(old_id[5]) - 1

    return f"ex1_par{participant_id}_ses{session_id}_per{performance_id}"

for expert_id in config:
    expert_config = config[expert_id]
    expert_results = results[expert_id]

    for i, pair in enumerate(expert_config):
        last_recording = new_id(pair[expert_results[i]])
        first_recording = new_id(pair[1 - expert_results[i]])

        if not "ordering" in info[first_recording]:
            info[first_recording]["ordering"] = {}
        if not "ordering" in info[last_recording]:
            info[last_recording]["ordering"] = {}
        
        info[first_recording]["ordering"][expert_id] = "first"
        info[last_recording]["ordering"][expert_id] = "last"




with open("info.json", "w") as f:
    json.dump(info, f, indent=4, cls=CompactJSONEncoder)