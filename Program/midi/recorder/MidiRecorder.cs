using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.midi;
using Godot;
using Program.util;

namespace Program.midi.recorder;

public partial class MidiRecorder : Node
{
	public MidiSong Song = new();

	public List<MidiNote> PendingNotes = [];
	
	public override void _Ready()
	{
		MidiServer.Instance.NoteSent += OnMidiServerNoteSent;
	}

	public void ProcessNoteEnd(MidiNote pendingNote, double endTime)
	{
		// Find measure of pending note, create empty measure(s)
		var measureNum = (int)Math.Truncate(pendingNote.Time);
		Song.Fill(measureNum + 1);

		// Change length and time of pending note
		Debug.Assert(endTime >= pendingNote.Time);
		pendingNote.Length = endTime - pendingNote.Time;
		pendingNote.Time -= measureNum;
			
		// Add pending note to measure
		var measure = Song.Measures[measureNum];
		measure.Notes.Add(pendingNote);
	}
	
	public void Flush(int newMeasureCount)
	{
		// Process all pending notes
		foreach (var pendingNote in PendingNotes)
			ProcessNoteEnd(pendingNote, newMeasureCount);
		PendingNotes.Clear();
        
		// Fill measures
		Song.Fill(newMeasureCount);
	}

	public void OnMidiServerNoteSent(MidiNote midiNote)
	{
		if (midiNote.Time < 0) return;

		if (midiNote.Velocity == 0)
		{
			// Find pending note, return if none found
			var findPendingNotes = PendingNotes
				.FindAll(note => note.OutputName == midiNote.OutputName && note.Note == midiNote.Note);
			if (findPendingNotes is not [var pendingNote])
			{
				Console.WriteLine("[MIDI RECORDER] Error in pending note (note off)");
				return;
			}

			// Remove note from pending list
			PendingNotes.Remove(pendingNote);

			// Add pending note to corresponding measure
			ProcessNoteEnd(pendingNote, midiNote.Time);
		}
		else
		{
			// Assert no similar pending notes exist
			if (PendingNotes.Any(note => note.OutputName == midiNote.OutputName && note.Note == midiNote.Note))
				Console.WriteLine("[MIDI RECORDER] Error in pending note (note on)");

			// Add note to pending list
			PendingNotes.Add(midiNote);
		}
	}
	
	public IEnumerable<MidiMeasure> GetUserMeasures(int count) => Song.Measures.TakeLast(count).Select(
		it => new MidiMeasure { Notes = it.Notes.Where(note => note.OutputName == OutputName.Loopback).ToList() }
	);

	public void Save()
	{
		// Get file name
		var init = GetNode("/root/PerformanceScreenInit");
		var standardName = (string)init.Get("standard_name");
		var soloistIndex = (int)init.Get("soloist");
		var dateTime = DateTime.Now.ToString("yyMMdd HHmm");
		var fileName = $"user://recordings/[{dateTime}] {standardName} {soloistIndex}.notes";
		
		// Save to stream
		Song.ToNotesFileStream(new FileAccessStream(fileName, FileAccess.ModeFlags.Write));

		// Print
		Console.WriteLine($"Saved to {fileName}");
	}
}