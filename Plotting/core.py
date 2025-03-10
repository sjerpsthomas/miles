from enum import IntEnum 
from dataclasses import dataclass

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
