from core import *
import numpy as np
from dataclasses import replace


# TODO: token metrics?


# UTILITY FUNCTIONS

def note_list(measures: list[Measure]) -> list[int]: return [note.note for measure in measures for note in measure.notes]

def length_list(measures: list[Measure]) -> list[int]: return [note.length for measure in measures for note in measure.notes]


# SIMPLE FEATURES

def note_std(measures: list[Measure]) -> float: return np.std(note_list(measures))

def length_std(measures: list[Measure]) -> float: return np.std(length_list(measures))

def note_range(measures: list[Measure]) -> float:
    notes: list[int] = note_list(measures)
    return np.max(notes) - np.min(notes)

def note_density(measures: list[Measure]) -> float:
    return sum(length_list(measures)) / len(measures)


# INTERVAL FEATURES

def interval_list(measures: list[Measure]) -> list[int]:
    return np.diff([note.note for measure in measures for note in measure.notes])

def interval_avg(measures: list[Measure]) -> float: return np.average(np.abs(interval_list(measures)))
def interval_std(measures: list[Measure]) -> float: return np.std(interval_list(measures))


# PITCH CLASS FEATURES

def note_share(measures: list[Measure], pitch_class: int) -> float:
    notes: list[int] = note_list(measures)
    return sum(note % 12 == pitch_class for note in notes) / len(notes)

def note_root_share(measures: list[Measure], key: int) -> float: return note_share(measures, key % 12)

def note_third_share(measures: list[Measure], key: int) -> float: return note_share(measures, (key + 4) % 12)

def note_fifth_share(measures: list[Measure], key: int) -> float: return note_share(measures, (key + 7) % 12)


# MELODIC ARC FEATURES

def all_notes(measures: list[Measure]) -> list[MidiNote]:
    return [replace(note, time=(note.time + i)) for i in range(len(measures)) for note in measures[i].notes]

def melodic_arc_list(measures: list[Measure]) -> list[tuple[float, float]]:
    intervals: list[int] = interval_list(measures)
    notes: list[MidiNote] = all_notes(measures)

    # Get places where interval switches sign
    boundaries = np.nonzero(np.diff(intervals))[0] + 1
    
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

def melodic_arc_duration_avg(measures: list[Measure]) -> float: return np.average([width for (width, _) in melodic_arc_list(measures)])

def melodic_arc_height_avg(measures: list[Measure]) -> float: return np.average([height for (_, height) in melodic_arc_list(measures)])

def extrema_ratio(measures: list[Measure]) -> float:
    # Get number of melodic arcs and note count
    melodic_arc_count: int = len(melodic_arc_list(measures))
    note_count: int = sum(len(measure.notes) for measure in measures)

    # Get number of notes that are reversals (and account for 'fencepost counting')
    return (melodic_arc_count - 1) / note_count


# OTHER METRICS

def ioi_list(measures: list[Measure]) -> list[float]:
    return np.diff([note.time for note in all_notes(measures)])

def ioi_avg(measures: list[Measure]) -> float: return np.average(ioi_list(measures))

def npvi(measures: list[Measure]) -> float:
    time_intervals = ioi_list(measures)
    note_count = len(time_intervals) + 1
    
    return (100 / (note_count - 2)) * sum(
        abs(
            (time_intervals[k] - time_intervals[k + 1]) /
            ((time_intervals[k] + time_intervals[k + 1]) / 2
        )
    ) for k in range(note_count - 2))
