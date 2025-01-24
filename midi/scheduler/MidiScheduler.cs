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
	public double CurrentTime = -1.0;
	
	public PriorityQueue<MidiNote, double> NoteQueue = new();

	public DateTime StartDateTime;
	public bool Enabled;

	public List<MidiSchedulerComponent> Components = new();
	
	public override void _Ready()
	{
		BPM = 169;
		
		Components.Add(new FileMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = Recorder,
			FileName = "res://midi/files/anotheryou_BACK.mid"
		});

		// TODO read from file
		var leadSheet = new LeadSheet()
		{
			Chords =
			[
				[new Chord(Eb, Major)], [new Chord(Eb, Major)], [new Chord(D, HalfDim7)], [new Chord(G, Dominant)],
				[new Chord(C, Minor)], [new Chord(C, Minor)], [new Chord(Bb, Minor)], [new Chord(Eb, Dominant)],
				[new Chord(Ab, Major)], [new Chord(Db, Dominant)], [new Chord(Eb, Major)], [new Chord(C, Minor)],
				[new Chord(F, Dominant)], [new Chord(F, Dominant)], [new Chord(F, Minor)], [new Chord(Bb, Dominant)],
				[new Chord(Eb, Major)], [new Chord(Eb, Major)], [new Chord(D, HalfDim7)], [new Chord(G, Dominant)],
				[new Chord(C, Minor)], [new Chord(C, Minor)], [new Chord(Bb, Minor)], [new Chord(Eb, Dominant)],
				[new Chord(Ab, Major)], [new Chord(Db, Dominant)], [new Chord(Eb, Major)], [new Chord(D, Major)],
				[new Chord(Eb, Major)], [new Chord(C, Dominant)], [new Chord(F, Minor)], [new Chord(Eb, Major)]
			],
			SoloDivision =
			[
				Learner, Learner, Learner, Learner,
				Algorithm, Algorithm, Algorithm, Algorithm,
				Learner, Learner, Learner, Learner,
				Algorithm, Algorithm, Algorithm, Algorithm,
				Learner, Learner, Learner, Learner,
				Algorithm, Algorithm, Algorithm, Algorithm,
				Learner, Learner, Learner, Learner,
				Algorithm, Algorithm, Algorithm, Algorithm,
			]
		};

		Components.Add(
			new SoloMidiSchedulerComponent(MidiSong.FromFile("res://midi/files/anotheryou_SOLO.mid"), leadSheet,
				new FactorOracleSoloist())
			{
				Scheduler = this,
				Recorder = Recorder,
			});
		
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