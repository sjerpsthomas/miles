using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using MeasureData = thesis.midi.scheduler.MidiScheduler.MeasureData;
using OutputName = thesis.midi.MidiServer.OutputName;
using NoteData = thesis.midi.scheduler.MidiScheduler.NoteData;

namespace thesis.midi.recorder;

public partial class MidiRecorder : Node
{
	public static MidiRecorder Instance;
	
	public List<MeasureData> Measures = new();
	
	public override void _Ready()
	{
		Instance = this;
		
		MidiServer.Instance.NoteSent += OnMidiServerNoteSent;
	}

	public void FillMeasures(int newMeasureCount)
	{
		while (Measures.Count < newMeasureCount)
			Measures.Add(new MeasureData());
	}

	public void ProcessPendingNote(NoteData pendingNote, double endTime)
	{
		// Find measure of pending note, create empty measure(s)
		var measureNum = (int)Math.Truncate(pendingNote.Time);
		FillMeasures(measureNum + 1);

		// Change length and time of pending note
		pendingNote.Length = endTime - pendingNote.Time;
		pendingNote.Time -= measureNum;
			
		// Add pending note to measure
		var measure = Measures[measureNum];
		measure.Notes.Add(pendingNote);
	}
	
	public void CutOff(int newMeasureCount)
	{
		// Process all pending notes
		foreach (var pendingNote in PendingNotes)
			ProcessPendingNote(pendingNote, newMeasureCount);
		PendingNotes.Clear();
        
		// Fill measures
		FillMeasures(newMeasureCount);
	}

	public List<NoteData> PendingNotes = new();
	
	public void OnMidiServerNoteSent(OutputName outputName, NoteData noteData)
	{
		if (outputName != OutputName.Loopback) return;
		
		if (noteData.Velocity > 0)
		{
			// Assert no pending notes exist
			var pendingNoteIndex = PendingNotes.FindLastIndex(note => note.Note == noteData.Note);
			Debug.Assert(pendingNoteIndex == -1);

			PendingNotes.Add(noteData);
		}
		else
		{
			// Find pending note, return if none found
			var findPendingNotes = PendingNotes.FindAll(note => note.Note == noteData.Note);
			if (findPendingNotes is not [var pendingNote]) return;
			
			PendingNotes.Remove(pendingNote);
			
			ProcessPendingNote(pendingNote, noteData.Time);
		}
	}
}