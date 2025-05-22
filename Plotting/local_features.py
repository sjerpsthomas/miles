from typing import Any

from numpy import ndarray, dtype

from recording import Measure, MidiNote
import numpy as np
from dataclasses import replace
from numpy.typing import NDArray


# TODO: token metrics?


# UTILITY FUNCTIONS

def note_list(measures: list[Measure]) -> list[int]: return [note.note for measure in measures for note in measure.notes]

def length_list(measures: list[Measure]) -> list[float]: return [note.length for measure in measures for note in measure.notes]

def to_bpm_independent(measures: list[Measure], bpm: float) -> list[MidiNote]:
    notes = all_notes(measures)
    return [
        replace(note, time=(note.time * 60 * (4 / bpm)), length=(note.length * 60 * (4 / bpm))) for note in notes
    ]

# SIMPLE FEATURES

def note_std(measures: list[Measure]) -> float: return float(np.std(note_list(measures)))

def length_std(measures: list[Measure]) -> float: return float(np.std(length_list(measures)))

def note_range(measures: list[Measure]) -> float:
    notes: list[int] = note_list(measures)
    return np.max(notes) - np.min(notes)

def note_density(measures: list[Measure]) -> float:
    return sum(length_list(measures)) / len(measures)


# INTERVAL FEATURES

def interval_list(measures: list[Measure]) -> NDArray:
    return np.diff([note.note for measure in measures for note in measure.notes])

def interval_avg(measures: list[Measure]) -> float: 
    intervals = np.abs(interval_list(measures))
    if len(intervals) == 0: return 0.0
    return np.average(intervals)
def interval_std(measures: list[Measure]) -> float: return float(np.std(interval_list(measures)))


# PITCH CLASS FEATURES

def note_share(measures: list[Measure], pitch_class: int) -> float:
    notes: list[int] = note_list(measures)
    return sum(note % 12 == pitch_class % 12 for note in notes) / len(notes)

def note_share_135(measures: list[Measure], pitch_classes: list[int]) -> float:
    notes: list[int] = note_list(measures)
    return sum(note % 12 in [p % 12 for p in pitch_classes] for note in notes) / len(notes)

# MELODIC ARC FEATURES

def all_notes(measures: list[Measure]) -> list[MidiNote]:
    return [replace(note, time=(note.time + i)) for i in range(len(measures)) for note in measures[i].notes]

def melodic_arc_list(measures: list[Measure], bpm: float) -> list[tuple[float, float]]:
    intervals: NDArray = interval_list(measures)
    notes: list[MidiNote] = to_bpm_independent(measures, bpm)

    # Get places where interval switches sign
    boundaries: NDArray = np.nonzero(np.diff(intervals))[0] + 1
    
    # Build arcs list based on boundaries
    arcs: list[list[MidiNote]] = []
    arc_start_index: int = 0

    # Iterate over boundaries
    boundary: int
    for boundary in boundaries:
        arcs.append(notes[arc_start_index:boundary + 1])
        arc_start_index = boundary
    
    # Append last arc
    arcs.append(notes[arc_start_index:])

    durations: list[float] = [((arc[-1].time + arc[-1].length) - arc[0].time) if arc != [] else 0.0 for arc in arcs]
    heights: list[float] = [float(np.max([note.note for note in arc]) - np.min([note.note for note in arc])) if arc != [] else 0.0 for arc in arcs]

    return list(zip(durations, heights))

def melodic_arc_duration_avg(measures: list[Measure], bpm: float) -> float: return float(np.average([width for (width, _) in melodic_arc_list(measures, bpm)]))

def melodic_arc_height_avg(measures: list[Measure], bpm: float) -> float: return float(np.average([height for (_, height) in melodic_arc_list(measures, bpm)]))

def extrema_ratio(measures: list[Measure], bpm: float) -> float:
    # Get number of melodic arcs and note count
    melodic_arc_count: int = len(melodic_arc_list(measures, bpm))
    note_count: int = sum(len(measure.notes) for measure in measures)

    # Get number of notes that are reversals (and account for 'fencepost counting')
    return (melodic_arc_count - 1) / note_count


# OTHER METRICS

def ioi_list(measures: list[Measure], bpm: float) -> NDArray:
    return np.diff([note.time for note in to_bpm_independent(measures, bpm)])

def ioi_avg(measures: list[Measure], bpm: float) -> float:
    return float(np.average(ioi_list(measures, bpm)))

def npvi(measures: list[Measure], bpm: float) -> float:
    time_intervals = ioi_list(measures, bpm)
    time_intervals = np.array([ioi for ioi in time_intervals if ioi > 0.01])
    note_count = len(time_intervals) + 1
    
    if note_count <= 2: return 0.0

    return (100 / (note_count - 2)) * sum(
        abs(
            (time_intervals[k] - time_intervals[k + 1]) /
            ((time_intervals[k] + time_intervals[k + 1]) / 2
        )
    ) for k in range(note_count - 2))
