import numpy as np
from operator import attrgetter
from notes_file import *
from dataclasses import replace, dataclass

NUM_MEASURES: int = 64


@dataclass
class Measure:
    notes: list[MidiNote]

    def of_output_name(self, output_name: OutputName) -> Self:
        notes: list[MidiNote] = list(filter(lambda note: note.output_name == output_name, self.notes))
        return Measure(notes)

class Recording:
    bpm: int
    measures: list[Measure]

    def __init__(self, file_name: str, num_measures: int = 64) -> None:
        with open(file_name, "rb") as f:
            io = MidiSongIO(f)
            song: MidiSong = io.read()

            self.bpm = song.bpm
            notes = song.get_solo()

            # Initialize empty measures
            self.measures = [Measure([]) for _ in range(num_measures)]

            # Append notes to measures
            for note in notes:
                measure_num: int = int(note.time)
                if measure_num >= num_measures: continue

                measure: Measure = self.measures[measure_num]
                relative_note = replace(note, time=(note.time - measure_num))

                measure.notes.append(relative_note)

            # Sort measures
            for measure in self.measures:
                measure.notes.sort(key=attrgetter('time'))

    @property
    def fours(self) -> list[list[Measure]]:
        asdf = np.split(np.array(self.measures), 64//4)

        return list(map(lambda x: list(x), asdf))

    @property
    def human_fours(self) -> list[list[Measure]]:
        res: list[list[Measure]] = []

        num_measures: int = len(self.measures)
        for i in range(0, num_measures, 8):
            measures_of_loopback = list(map(lambda measure: measure.of_output_name(OutputName.LOOPBACK), self.measures[i:i+4]))
            res.append(measures_of_loopback)
        
        return res

    @property
    def agent_fours(self) -> list[list[Measure]]:
        res: list[list[Measure]] = []

        num_measures: int = len(self.measures)
        for i in range(4, num_measures, 8):
            measures_of_algorithm = list(map(lambda measure: measure.of_output_name(OutputName.ALGORITHM), self.measures[i:i+4]))
            res.append(measures_of_algorithm)
        
        return res
