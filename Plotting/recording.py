from typing import Self
from binreader import BinaryReader
from operator import attrgetter
from enum import IntEnum
from dataclasses import *


NUM_MEASURES: int = 64

# (Python port of OutputName.cs)
class OutputName(IntEnum):
    LOOPBACK = 0,
    ALGORITHM = 1
    METRONOME = 2
    BACKING1BASS = 3
    BACKING2PIANO = 4
    BACKING3KEYBOARD = 5
    BACKING4DRUMS = 6
    UNKNOWN = 7

# (Python port of MidiNote.cs)
@dataclass
class MidiNote:
    output_name: OutputName
    time: float
    note: int
    length: float
    velocity: int


@dataclass
class Measure:
    notes: list[MidiNote]

    def of_output_name(self, output_name: OutputName) -> Self:
        notes: list[MidiNote] = list(filter(lambda note: note.output_name == output_name, self.notes))
        return Measure(notes)


class Recording:
    measures: list[Measure]

    def __init__(self, file_name: str) -> None:
        with open(file_name, "rb") as f:
            reader: BinaryReader = BinaryReader(f)

            # Read count
            count: int = reader.read_int32()

            # Read notes
            notes: list[MidiNote] = [
                MidiNote(
                    output_name=OutputName(reader.read_byte()),
                    time=reader.read_double(),
                    length=reader.read_double(),
                    note=reader.read_int32(),
                    velocity=reader.read_int32(),
                )
                for _ in range(count)
            ]

            # Initialize empty measures
            self.measures = [Measure([]) for _ in range(NUM_MEASURES)]

            # Append notes to measures
            for note in notes:
                measure_num: int = int(note.time)
                if measure_num >= NUM_MEASURES: continue

                measure: Measure = self.measures[measure_num]
                relative_note = replace(note, time=(note.time - measure_num))

                measure.notes.append(relative_note)

            # Sort measures
            for measure in self.measures:
                measure.notes.sort(key=attrgetter('time'))

    @property
    def human_fours(self) -> list[list[Measure]]:
        res: list[list[Measure]] = []

        for i in range(0, NUM_MEASURES, 8):
            measures_of_loopback = list(map(lambda measure: measure.of_output_name(OutputName.LOOPBACK), self.measures[i:i+4]))
            res.append(measures_of_loopback)
        
        return res

    @property
    def agent_fours(self) -> list[list[Measure]]:
        res: list[list[Measure]] = []

        for i in range(4, NUM_MEASURES, 8):
            measures_of_algorithm = list(map(lambda measure: measure.of_output_name(OutputName.ALGORITHM), self.measures[i:i+4]))
            res.append(measures_of_algorithm)
        
        return res
