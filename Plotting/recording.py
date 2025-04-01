import numpy as np
from operator import attrgetter
from notes_file import *
from dataclasses import replace

NUM_MEASURES: int = 64

class Recording:
    measures: list[Measure]

    def __init__(self, file_name: str) -> None:
        with open(file_name, "rb") as f:
            note_file: NoteFile = NoteFile(f)
            notes: list[MidiNote] = note_file.read_notes()

            notes = [note for note in notes if note.output_name in [OutputName.LOOPBACK, OutputName.ALGORITHM]]

            for i in range(len(notes) - 1):
                note: MidiNote = notes[i]
                next_note: MidiNote = notes[i + 1]

                ioi: float = next_note.time - note.time
                note.length = min(ioi, note.length)

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
    def fours(self) -> list[list[Measure]]:
        asdf = np.split(np.array(self.measures), 64//4)

        return list(map(lambda x: list(x), asdf))

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
