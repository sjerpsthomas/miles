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

[
    [[3.0, 3.0000000000000004, 3.0], [4.0, 3.333333333333333, 3.555555555555556], [1.3333333333333333, 1.0, 1.1111111111111112], [2.3333333333333335, 1.5, 1.777777777777778]],
    [[3.333333333333333, 2.5, 2.7777777777777777], [4.0, 3.5, 3.666666666666666], [3.333333333333333, 2.5, 2.7777777777777777], [3.666666666666666, 2.666666666666667, 3.0]],
    [[3.333333333333333, 1.6666666666666665, 2.2222222222222223], [3.666666666666667, 2.3333333333333335, 2.7777777777777777], [2.0, 1.8333333333333335, 1.8888888888888888], [2.333333333333333, 2.0, 2.111111111111111]],
    [[4.0, 2.3333333333333335, 2.888888888888889], [4.666666666666667, 2.6666666666666665, 3.333333333333333], [2.6666666666666665, 1.833333333333333, 2.111111111111111], [4.0, 1.6666666666666665, 2.444444444444444]]
]