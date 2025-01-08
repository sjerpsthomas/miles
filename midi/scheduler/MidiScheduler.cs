using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using thesis.midi.recorder;
using thesis.midi.scheduler.component;
using static thesis.midi.MidiServer.OutputName;

namespace thesis.midi.scheduler;

public partial class MidiScheduler : Node
{
	public static MidiScheduler Instance;
	
	public record struct NoteData(double Time, double Length, int Note, int Velocity);

	public class MeasureData
	{
		public List<NoteData> Notes;

		public MeasureData(params NoteData[] notes)
		{
			Notes = notes.ToList();
		}
	}
	
	// ReSharper disable once InconsistentNaming
	public double BPM;

	// Integer part: measure
	// Fractional part: part within measure
	public double CurrentTime = -1.0;
	
	public PriorityQueue<NoteData, double> NoteQueue = new();

	public DateTime StartDateTime;
	public bool Enabled;

	public List<MidiSchedulerComponent> Components = new();
	
	public override void _Ready()
	{
		Instance = this;
		
		BPM = 150;

		Components.Add(new MetronomeMidiSchedulerComponent { Scheduler = this, Recorder = MidiRecorder.Instance });
		Components.Add(new RepeaterMidiSchedulerComponent { Scheduler = this, Recorder = MidiRecorder.Instance });

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
			MidiRecorder.Instance.FillMeasures(currentMeasure);
			
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

	public void AddMeasure(int measure, MeasureData data)
	{
		lock (NoteQueue)
		{
			foreach (var note in data.Notes)
			{
				NoteQueue.Enqueue(note, note.Time + measure);

				// if (note.Note != 70)
				// 	GD.Print(note);
				
				// Add note off
				if (note.Length == 0.0) continue;
				if (note.Velocity <= 0) continue;
				var noteOffTime = note.Time + note.Length;
				NoteQueue.Enqueue(new NoteData(noteOffTime, 0, note.Note, 0), noteOffTime + measure);
				
				// if (note.Note != 70)
				// 	GD.Print("Add off!");
			}
		}
	}
}
