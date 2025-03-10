from core import *
from typing import Generator
from binreader import BinaryReader
from operator import attrgetter
import functools
import dataclasses

# (Python port of MidiSong.cs::FromNotesFileStream: Reads a list of MidiNotes from file)
def load_notes(file_name: str) -> Generator[MidiNote, any, None]:
    with open(file_name, "rb") as f:
        reader: BinaryReader = BinaryReader(f)

        # Read count
        count: int = reader.read_int32()

        # Read notes
        for _ in range(count):
            yield MidiNote(
                output_name=OutputName(reader.read_byte()),
                time=reader.read_double(),
                length=reader.read_double(),
                note=reader.read_int32(),
                velocity=reader.read_int32(),
            )

NUM_MEASURES: int = 64



class Performance:
    measures: list[Measure]

    def __init__(self, notes: list[MidiNote]) -> None:
        # Initialize empty measures
        self.measures = NUM_MEASURES * [Measure([])]

        # Append notes to measures
        for note in notes:
            measure_num: int = int(note.time)
            measure: Measure = self.measures[measure_num]
            relative_note = dataclasses.replace(note, time=(note.time - measure_num))

            measure.notes.append(relative_note)
        
        # Sort measures
        for measure in self.measures:
            measure.notes.sort(key=attrgetter('time'))
    
    @functools.cached_property
    def human_fours(self):# -> list[list[Measure]]:
        res: list[list[Measure]] = []

        for i in range(0, NUM_MEASURES, 4):
            res.append(self.measures[i:i+4])
        
        return res

    @functools.cached_property
    def agent_fours(self) -> list[list[Measure]]:
        res: list[list[Measure]] = []

        for i in range(4, NUM_MEASURES, 4):
            res.append(self.measures[i:i+4])
        
        return res
