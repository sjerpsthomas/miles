import json
from compact_json_encoder import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)


NUM_SCORES = 2
NUM_SESSIONS = 4
NUM_QUESTIONS = 4


class Avg:
    x: float
    n: float

    def __init__(self):
        self.x = 0.0
        self.n = 0.0

    def avg(self): return self.x / self.n

    def __repr__(self): return "-" if self.n == 0 else f"{self.avg():.1f}"


table = [
    [
        [Avg() for _ in range(NUM_SCORES)]
        for _ in range(NUM_SESSIONS)
    ]   for _ in range(NUM_QUESTIONS)
]


config: str
for config in info:
    if not config.startswith("ex1"): continue

    participant_id: int = int(config[7])
    session_id: int = int(config[12])

    

    performance_id: int = int(config[17])


    for q in range(NUM_QUESTIONS):
        table[q][session_id][0].x += info[config]["scores"]["-1"][q]
        table[q][session_id][0].n += 1

        # Skip fourth session (not rated)
        if session_id == 3: continue

        table[q][session_id][1].x += sum([
            info[config]["scores"][str(ex)][q]
            for ex in range(3)
        ])
        table[q][session_id][1].n += 3


print(table)

# [
#     [[2.8, 1.9], [2.5, 1.6], [3.1, 1.5], [3.2, -]],
#     [[3.3, 2.4], [3.2, 2.4], [3.0, 2.2], [3.5, -]],
#     [[3.5, 2.1], [2.7, 2.1], [3.4, 1.8], [3.2, -]],
#     [[3.9, 2.4], [3.2, 2.2], [3.6, 2.0], [4.0, -]]
# ]
