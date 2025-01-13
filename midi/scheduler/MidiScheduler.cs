using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using thesis.midi.core;
using thesis.midi.recorder;
using thesis.midi.scheduler.component;
using static thesis.midi.MidiServer.OutputName;

namespace thesis.midi.scheduler;

public partial class MidiScheduler : Node
{
	public static MidiScheduler Instance;

	// ReSharper disable once InconsistentNaming
	public double BPM;

	// Integer part: measure
	// Fractional part: part within measure
	public double CurrentTime = -1.0;
	
	public PriorityQueue<MidiNote, double> NoteQueue = new();

	public DateTime StartDateTime;
	public bool Enabled;

	public List<MidiSchedulerComponent> Components = new();
	
	public override void _Ready()
	{
		Instance = this;
		
		BPM = 150;

		Components.Add(new MetronomeMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = MidiRecorder.Instance
		});
		Components.Add(new RepeaterMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = MidiRecorder.Instance
		});
		Components.Add(new FileMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = MidiRecorder.Instance,
			FileName = "res://midi/files/turkish_march.mid"
		});
        
		Start();
	}

	public void Start()
	{
		Enabled = true;
		
		var thread = new Thread(Run);
		thread.Start();
	}
	
	public void Run()
	{
		StartDateTime = DateTime.Now;
		
		while (Enabled)
		{
			var currentTime = GetTime(DateTime.Now);
			Tick(currentTime);
		}	
	}

	public double GetTime(DateTime dateTime)
	{
		var elapsed = dateTime - StartDateTime;
		double currentTimeMs = elapsed.TotalMilliseconds;
		return currentTimeMs / 1000.0 / (60.0 / BPM) / 4.0;
	}
    
	public void Tick(double newCurrentTime)
	{
		// Call components every measure
		var currentMeasure = (int)Math.Truncate(newCurrentTime);
		if ((int)Math.Truncate(CurrentTime) != currentMeasure)
		{
			foreach (var component in Components)
				component.HandleMeasure(currentMeasure);
		}
		
		// Update current time
		CurrentTime = newCurrentTime;
		
		// Play notes when needed
		lock (NoteQueue)
			while (NoteQueue.TryPeek(out _, out var time) && time < CurrentTime)
				MidiServer.Instance.Send(Algorithm, NoteQueue.Dequeue());
	}

	public void AddMeasure(int measureNum, MidiMeasure measure)
	{
		lock (NoteQueue)
		{
			foreach (var note in measure.Notes)
			{
				NoteQueue.Enqueue(note, note.Time + measureNum);

				// Add note off
				if (note.Length == 0.0) continue;
				if (note.Velocity <= 0) continue;
				var noteOffTime = note.Time + note.Length;
				NoteQueue.Enqueue(new MidiNote(noteOffTime, 0, note.Note, 0), noteOffTime + measureNum);
			}
		}
	}

	public void AddSong(int startMeasureNum, MidiSong song)
	{
		lock (NoteQueue)
		{
			for (var index = 0; index < song.Measures.Count; index++)
				AddMeasure(startMeasureNum + index, song.Measures[index]);
		}
	}
}