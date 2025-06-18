import json

# Import recording (hack)
import sys
import os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))
from recording import Recording


ALGORITHM_LONG_NAMES = [
    "Token Factor Oracle v1",
    "Token Factor Oracle v2",
    "Token Markov v1",
    "Token Markov v2",
]

ALGORITHM_SHORT_NAMES = [
    "TFOv1",
    "TFOv2",
    "TMv1",
    "TMv2",
]

NUM_PARTICIPANTS = 3
NUM_ALGORITHMS = 4
NUM_QUESTIONS = 4


class Performance:
    participant_id: int
    performance_id: int

    song: int
    algorithm: int
    scores: dict[int, list[int]]
    edit_distances: list[int]
    recording: Recording
    comment: str

    def __init__(self, id: str, data: dict) -> None:
        self.participant_id = int(id[7])
        self.performance_id = int(id[12])

        self.song = data["song"]
        self.algorithm = data["algorithm"]
        self.scores = { int(p): data["scores"][p] for p in data["scores"] }
        self.edit_distances = data["edit_distances"]
        self.recording = Recording(f"recordings/{data["path"]}", num_measures=48)
        self.comment = data["comment"]



class Info:
    info: dict[str, Performance]
    experiment: int

    def __init__(self, file_name: str = "recordings/info.json", experiment: int = 2):
        with open(file_name) as f:
            json_info = json.load(f)

        self.experiment = experiment
        self.info = { id: Performance(id, json_info[id]) for id in json_info if id.startswith(f"ex{experiment}") }

    def data(self, participant: int, performance: int):
        return self.info[f"ex{self.experiment}_par{participant}_per{performance}"]
