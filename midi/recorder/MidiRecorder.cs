using System;
using System.Collections.Generic;
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
	
	public void OnMidiServerNoteSent(OutputName outputName, NoteData noteData)
	{
		if (outputName != OutputName.Loopback) return;
		
		if (noteData.Velocity > 0)
		{
			var measure = (int)Math.Truncate(noteData.Time);
			
			// Create new measures
			FillMeasures(measure + 1);
			
			// Populate the last measure
			var lastMeasure = Measures[^1];

			noteData.Time -= measure;
			lastMeasure.Notes.Add(noteData);
		}
		else
		{
			// Find corresponding 'on' note
			var lastMeasure = Measures.Count - 1;
			var lastPlayedMeasure = Measures[lastMeasure];
			var lastNoteIndex = lastPlayedMeasure.Notes.FindLastIndex(note => note.Note == noteData.Note && note.Length == 0.0);

			if (lastNoteIndex == -1) return;
			var lastNote = lastPlayedMeasure.Notes[lastNoteIndex];

			lastNote.Length = (noteData.Time) - (lastNote.Time + lastMeasure);

			lastPlayedMeasure.Notes[lastNoteIndex] = lastNote;
		}
	}
}