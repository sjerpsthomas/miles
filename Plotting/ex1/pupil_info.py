from dataclasses import dataclass
import json

# Import recording (hack)
import sys
import os
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))
from recording import Recording

# IDs of songs
SONG_IDS: dict[str, int] = {
    "Long Ago and Far Away": 0,
    "Summertime": 1,
    "My Little Suede Shoes": 2
}

# Names of songs
SONG_NAMES: dict[int, str] = {
    0: "Long Ago and Far Away",
    1: "Summertime",
    2: "My Little Suede Shoes",
}

# IDs of algorithms
ALGORITHM_IDS: dict[str, int] = {
    "Note Retrieval": 0,
    "Token Factor Oracle v1": 1,
    "Token Markov v1": 2,
}

# Names of algorithms
ALGORITHM_NAMES: dict[int, str] = {
    0: "Note Retrieval",
    1: "Token Factor Oracle v1",
    2: "Token Markov v1",
}

# Pitch classes of root, third and fifth of songs
SONG_PITCH_CLASSES: dict[str, list[int]] = {
    "Long Ago and Far Away": [7, 11, 2],
    "Summertime": [7, 10, 2],
    "My Little Suede Shoes": [3, 7, 10]
}

SONG_BPMS = {
    "Long Ago and Far Away": 178,
    "Summertime": 84,
    "My Little Suede Shoes": 146
}

NUM_PUPILS = 5
NUM_PERFORMANCES = 3
NUM_SONGS = 3
NUM_ALGORITHMS = 3
NUM_SESSIONS = 4
NUM_QUESTIONS = 4

NUM_EXPERTS = 3

@dataclass
class PerformanceInfo:
    song: int
    algorithm: int
    recording: Recording
    scores: list[int]
    expert_scores: list[list[int]]
    edit_distances: list[int]
    ordering: dict[int, str] | None

@dataclass
class SessionInfo:
    performances: list[PerformanceInfo]

@dataclass
class PupilInfo:
    sessions: list[SessionInfo]


# (Retrieves pupil info from spreadsheet.xlsx)
def get_all_pupil_info(file_name: str = "recordings/info.json") -> list[PupilInfo]:
    file = open(file_name, "r")
    info = json.load(file)

    res: list[PupilInfo] = []

    # For every pupil
    for pupil in range(NUM_PUPILS):
        pupil_info: PupilInfo = PupilInfo([])

        # For every session they do
        for session in range(NUM_SESSIONS):
            session_info: SessionInfo = SessionInfo([])

            # For every performance they do within that session
            for performance in range(NUM_PERFORMANCES):
                # Get info
                json_performance: dict[str, any] = info[f"ex1_par{pupil}_ses{session}_per{performance}"]

                song: int = json_performance["song"]
                algorithm: int = json_performance["algorithm"]
                scores: list[int] = json_performance["scores"]["-1"]

                expert_scores: list[list[int]] = [json_performance["scores"][str(i)] for i in json_performance["scores"] if i != "-1"]
                ordering: dict[int, str] | None = None
                if "ordering" in json_performance:
                    ordering = {int(k): json_performance["ordering"][k] for k in json_performance["ordering"]}

                edit_distances: list[int] = json_performance["edit_distances"]
                recording: Recording = Recording("recordings/" + json_performance["path"])

                # Create performance info, add
                performance_info: PerformanceInfo = PerformanceInfo(song, algorithm, recording, scores, expert_scores, edit_distances, ordering)
                session_info.performances.append(performance_info)
            
            pupil_info.sessions.append(session_info)
        
        res.append(pupil_info)
    
    file.close()
    return res
