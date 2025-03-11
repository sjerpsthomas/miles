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

def interval_list(measures: list[Measure]) -> list[int]
    return np.diff(note.note for measure in measures for note in measure.notes)

def interval_avg(measures: list[Measure]) -> float: return np.average(interval_list(measures))
def interval_std(measures: list[Measure]) -> float: return np.std(interval_list(measures))


# PITCH CLASS FEATURES

def note_share(measures: list[Measure], pitch_class: int) -> float:
    notes: list[int] = note_list(measures)
    return len(filter(lambda x: x % 12 == pitch_class)) / len(notes)

def note_root_share(measures: list[Measure], key: int) -> float: return note_share(measures, key % 12)

def note_third_share(measures: list[Measure], key: int) -> float: return note_share(measures, (key + 4) % 12)

def note_fifth_share(measures: list[Measure], key: int) -> float: return note_share(measures, (key + 7) % 12)


# MELODIC ARC FEATURES

def melodic_arc_list(measures: list[Measure]) -> list[tuple[float, float]]:
    return []

def melodic_arc_size_avg(measures: list[Measure]) -> float: return np.average(width for (width, _) in melodic_arc_list(measures))

def melodic_arc_size_avg(measures: list[Measure]) -> float: return np.average(height for (_, height) in melodic_arc_list(measures))

def extrema_ratio(measures: list[Measure]) -> float:
    # Get number of melodic arcs and note count
    melodic_arc_count: int = len(melodic_arc_list(measures))
    note_count: int = sum(len(measure.notes) for measure in measures)

    # Get number of notes that are reversals (and account for 'fencepost counting')
    return (melodic_arc_count - 1) / note_count


# OTHER METRICS

def npvi(measures: list[Measure]) -> float:
    all_notes: list[MidiNote] = (replace(note, time=(note.time + i)) for i in range(measures) for note in measures[i].notes)
    note_count: int = len(all_notes)

    time_intervals: list[float] = []

    for i in range(note_count - 1):
        note: MidiNote = all_notes[i]
        next_note: MidiNote = all_notes[i + 1]

        time_interval = next_note.time - (note.time + note.length)
        time_interval = max(time_interval, 0)

        time_intervals.append(time_interval)
    
    return (100 / (note_count - 2)) * sum(
        abs(
            (time_intervals[k] - time_intervals[k + 1]) /
            ((time_intervals[k] + time_intervals[k + 1]) / 2
        )
    ) for k in range(note_count - 2))
