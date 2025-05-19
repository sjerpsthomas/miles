import json
import numpy as np

with open("recordings/info.json") as f:
    info: object = json.load(f)

NUM_GROUPS = 3
NUM_ALGORITHMS = 4
NUM_PARTICIPANTS = 3
NUM_QUESTIONS = 4

results = [
    [
        [
            0.0 for _ in range(NUM_GROUPS)
        ] for _ in range(NUM_ALGORITHMS)
    ] for _ in range(NUM_QUESTIONS)
]


# Go over all relevant entries in info
id: str
for id in info:
    if not id.startswith("ex2"): continue
    performance: object = info[id]

    # Get the participant ID
    participant_id: int = int(id[7])

    # Go over all questions
    question_id: int
    for question_id in range(NUM_QUESTIONS):
        algorithm_id: int = performance["algorithm"]

        # Keep track of:
        
        # The answer the peer themselves gave
        self_reported: float = (performance["scores"][str(participant_id)][question_id])

        # The average answer their peers gave
        peer_reported: float = float(np.average([
            performance["scores"][str(pid)][question_id]
            for pid in range(NUM_PARTICIPANTS)
            if pid != participant_id
        ]))

        # The average answer all participants gave
        all_reported: float = float(np.average([
            performance["scores"][str(pid)][question_id]
            for pid in range(NUM_PARTICIPANTS)
        ]))

        # Set results
        results[question_id][algorithm_id][0] += self_reported / NUM_PARTICIPANTS
        results[question_id][algorithm_id][1] += peer_reported / NUM_PARTICIPANTS
        results[question_id][algorithm_id][2] += all_reported / NUM_PARTICIPANTS


print(results)

