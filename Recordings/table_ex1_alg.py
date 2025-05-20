import json
from compact_json_encoder import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)


NUM_SCORES = 2
NUM_ALGORITHMS = 3
NUM_QUESTIONS = 4


class Avg:
    x: float
    n: float

    def __init__(self):
        self.x = 0.0
        self.n = 0.0

    def avg(self): return self.x / self.n

    def __repr__(self): return f"{self.avg():.1f}"


table = [
    [
        [Avg() for _ in range(NUM_SCORES)]
        for _ in range(NUM_ALGORITHMS)
    ]   for _ in range(NUM_QUESTIONS)
]


config: str
for config in info:
    if not config.startswith("ex1"): continue

    participant_id: int = int(config[7])
    session_id: int = int(config[12])

    # Skip fourth session (not rated)
    if session_id == 3: continue

    performance_id: int = int(config[17])

    algorithm_id: int = info[config]["algorithm"]

    for q in range(NUM_QUESTIONS):
        table[q][algorithm_id][0].x += info[config]["scores"]["-1"][q]
        table[q][algorithm_id][0].n += 1

        table[q][algorithm_id][1].x += sum([
            info[config]["scores"][str(ex)][q]
            for ex in range(3)
        ])
        table[q][algorithm_id][1].n += 3


print(table)

# [
#     [[3.2, 1.7], [2.8, 1.9], [2.4, 1.3]],
#     [[3.1, 2.3], [3.1, 2.4], [3.2, 2.4]],
#     [[4.3, 1.8], [2.7, 2.2], [2.7, 2.0]],
#     [[4.1, 2.2], [3.2, 2.3], [3.4, 2.0]]
# ]