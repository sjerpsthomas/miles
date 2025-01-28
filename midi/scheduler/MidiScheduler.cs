using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using thesis.midi.core;
using thesis.midi.recorder;
using thesis.midi.scheduler.component;
using thesis.midi.scheduler.component.solo;
using static thesis.midi.core.Chord.KeyEnum;
using static thesis.midi.core.Chord.TypeEnum;
using static thesis.midi.core.LeadSheet.SoloType;

namespace thesis.midi.scheduler;

public partial class MidiScheduler : Node
{
	[Export] public MidiRecorder Recorder;
	
	// ReSharper disable once InconsistentNaming
	public double BPM;

	// Integer part: measure
	// Fractional part: part within measure
	public double CurrentTime = -2.0;
	
	public PriorityQueue<MidiNote, double> NoteQueue = new();

	public DateTime StartDateTime;
	public bool Enabled;

	public List<MidiSchedulerComponent> Components = new();
	
	public override void _Ready()
	{
		// Get path of standard
		var init = GetNode("/root/PerformanceScreenInit");
		var standardName = (string)init.Get("standard_name");
		var standardPath = $"user://saves/{standardName}/";

		// Load necessary files
		var backingTrack = MidiSong.FromFile(standardPath + "back.mid");
		var soloTrack = MidiSong.FromFile(standardPath + "solo.mid");
		var leadSheet = LeadSheet.FromFile(standardPath + "sheet.json");
		
		// Get BPM
		BPM = leadSheet.BPM;
		
		// Add components
		Components.Add(new SongMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = Recorder,
			Song = backingTrack
		});

		Soloist soloist = (int)init.Get("soloist") switch
		{
			0 => new FactorOracleSoloist(),
			1 => new RandomSoloist(),
			2 => new RetrievalSoloist(),
			_ => throw new ArgumentOutOfRangeException()
		};
        
		Components.Add(new SoloMidiSchedulerComponent(soloTrack, leadSheet, soloist)
		{
			Scheduler = this,
			Recorder = Recorder,
		});

		AddMeasure(-2, new MidiMeasure([
			new MidiNote(MidiServer.OutputName.Metronome, 0.0, 0.2, 22, 48),
			new MidiNote(MidiServer.OutputName.Metronome, 0.5, 0.2, 22, 48),
		]));
		
		AddMeasure(-1, new MidiMeasure([
			new MidiNote(MidiServer.OutputName.Metronome, 0.00, 0.2, 22, 48),
			new MidiNote(MidiServer.OutputName.Metronome, 0.25, 0.2, 22, 48),
			new MidiNote(MidiServer.OutputName.Metronome, 0.50, 0.2, 22, 48),
			new MidiNote(MidiServer.OutputName.Metronome, 0.75, 0.2, 22, 48),
		]));
		
		// Start
		Start();
	}

	public void Start()
	{
		MidiServer.Instance.Scheduler = this;
		Enabled = true;
		
		var thread = new Thread(Run);
		thread.Start();
	}

	public void Stop()
	{
		MidiServer.Instance.Scheduler = null;
		Enabled = false;
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
		var currentMeasure = currentTimeMs / 1000.0 / (60.0 / BPM) / 4.0;
		
		return currentMeasure - 2;
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
				MidiServer.Instance.Send(NoteQueue.Dequeue());
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
				NoteQueue.Enqueue(new MidiNote(note.OutputName, noteOffTime, 0, note.Note, 0), noteOffTime + measureNum);
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