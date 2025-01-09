using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using MeasureData = thesis.midi.scheduler.MidiScheduler.MeasureData;
using OutputName = thesis.midi.MidiServer.OutputName;
using NoteData = thesis.midi.scheduler.MidiScheduler.NoteData;

namespace thesis.midi.recorder;

public partial class MidiRecorder : Node
{
	public static MidiRecorder Instance;
	
	public List<MeasureData> Measures = [];
	
	public List<NoteData> PendingNotes = [];
	
	public override void _Ready()
	{
		Instance = this;
		
		MidiServer.Instance.NoteSent += OnMidiServerNoteSent;
	}

	public void FillMeasures(int newMeasureCount)
	{
		// Create new measures until count is reached
		while (Measures.Count <= newMeasureCount)
			Measures.Add(new MeasureData());
	}

	public void ProcessPendingNote(NoteData pendingNote, double endTime)
	{
		// Find measure of pending note, create empty measure(s)
		var measureNum = (int)Math.Truncate(pendingNote.Time);
		FillMeasures(measureNum);

		// Change length and time of pending note
		Debug.Assert(endTime >= pendingNote.Time);
		pendingNote.Length = endTime - pendingNote.Time;
		pendingNote.Time -= measureNum;
			
		// Add pending note to measure
		var measure = Measures[measureNum];
		measure.Notes.Add(pendingNote);
	}
	
	public void Flush(int newMeasureCount)
	{
		// Process all pending notes
		foreach (var pendingNote in PendingNotes)
			ProcessPendingNote(pendingNote, newMeasureCount);
		PendingNotes.Clear();
        
		// Fill measures
		FillMeasures(newMeasureCount - 1);
	}

	public void OnMidiServerNoteSent(OutputName outputName, NoteData noteData)
	{
		if (outputName != OutputName.Loopback) return;
		
		if (noteData.Velocity > 0)
		{
			// Assert no similar pending notes exist
			var pendingNoteIndex = PendingNotes.FindLastIndex(note => note.Note == noteData.Note);
			Debug.Assert(pendingNoteIndex == -1);

			// Add note to pending list
			PendingNotes.Add(noteData);
		}
		else
		{
			// Find pending note, return if none found
			var findPendingNotes = PendingNotes.FindAll(note => note.Note == noteData.Note);
			if (findPendingNotes is not [var pendingNote]) return;
			
			// Remove note from pending list
			PendingNotes.Remove(pendingNote);
			
			// Add pending note to corresponding measure
			ProcessPendingNote(pendingNote, noteData.Time);
		}
	}
}