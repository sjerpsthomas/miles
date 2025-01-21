using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using thesis.midi.core;
using OutputName = thesis.midi.MidiServer.OutputName;

namespace thesis.midi.recorder;

public partial class MidiRecorder : Node
{
	public MidiSong Song = new();
	
	public List<MidiNote> PendingNotes = [];
	
	public override void _Ready()
	{
		MidiServer.Instance.NoteSent += OnMidiServerNoteSent;
	}

	public void ProcessPendingNote(MidiNote pendingNote, double endTime)
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
			ProcessPendingNote(pendingNote, newMeasureCount);
		PendingNotes.Clear();
        
		// Fill measures
		Song.Fill(newMeasureCount);
	}

	public void OnMidiServerNoteSent(OutputName outputName, MidiNote midiNote)
	{
		if (outputName != OutputName.Loopback) return;
		
		if (midiNote.Velocity > 0)
		{
			// Assert no similar pending notes exist
			var pendingNoteIndex = PendingNotes.FindLastIndex(note => note.Note == midiNote.Note);
			Debug.Assert(pendingNoteIndex == -1);

			// Add note to pending list
			PendingNotes.Add(midiNote);
		}
		else
		{
			// Find pending note, return if none found
			var findPendingNotes = PendingNotes.FindAll(note => note.Note == midiNote.Note);
			if (findPendingNotes is not [var pendingNote]) return;
			
			// Remove note from pending list
			PendingNotes.Remove(pendingNote);
			
			// Add pending note to corresponding measure
			ProcessPendingNote(pendingNote, midiNote.Time);
		}
	}
}