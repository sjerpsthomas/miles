import struct
from typing import BinaryIO, Self
from dataclasses import dataclass
from enum import IntEnum


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
class MidiSong:
	bpm: int
	notes: list[MidiNote]

	def get_backing(self) -> list[MidiNote]:
		return [note for note in self.notes if note.output_name not in [OutputName.LOOPBACK, OutputName.ALGORITHM]]
	
	def get_solo(self) -> list[MidiNote]:
		return [note for note in self.notes if note.output_name in [OutputName.LOOPBACK, OutputName.ALGORITHM]]


class MidiSongIO:
	def __init__(self, buf: BinaryIO) -> None:
		self.buf = buf
		self.endian = "<"

	# READING
	def read(self) -> MidiSong:
		bpm = self._read_int()

		count = self._read_int()
		notes = [
			MidiNote(
				output_name=self._read_output_name(),
				time=self._read_double(),
				length=self._read_double(),
				note=self._read_int(),
				velocity=self._read_int()
			) for _ in range(count)
		]

		return MidiSong(bpm, notes)

	def _read(self, type: str, size: int): return struct.unpack(self.endian + type, self.buf.read(size))[0]

	def _read_output_name(self) -> OutputName: return OutputName(self._read('b', 1))
	def _read_int(self) -> int: return self._read('i', 4)
	def _read_double(self) -> float: return self._read('d', 8)

	# WRITING
	def write(self, song: MidiSong) -> None:
		self._write_int(song.bpm)
		
		self._write_int(len(song.notes))
		for note in song.notes:
			self._write_output_name(note.output_name)
			self._write_double(note.time)
			self._write_double(note.length)
			self._write_int(note.note)
			self._write_int(note.velocity)


	def _write(self, v, type: str): self.buf.write(struct.pack(self.endian + type, v))

	def _write_output_name(self, value: OutputName) -> None: self._write(int(value), 'b')
	def _write_int(self, value: int) -> None: self._write(value, 'i')
	def _write_double(self, value: float) -> None: self._write(value, 'd')