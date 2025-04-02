using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Core.midi;
using Core.midi.token;
using Godot;
using Program.midi.recorder;
using Program.midi.scheduler.component;
using Program.midi.scheduler.component.solo;
using Program.screen.performance;
using Program.util;
using static Godot.FileAccess.ModeFlags;

namespace Program.midi.scheduler;

public partial class MidiScheduler : Node
{
	[Export] public MidiRecorder Recorder;
	
	public int Bpm;

	// Integer part: measure
	// Fractional part: part within measure
	public double Time = -2.0;
	public int StartMeasure = 0;
	
	public PriorityQueue<MidiNote, double> NoteQueue = new();

	public DateTime StartDateTime;
	public int SongLength;
	public int Repetitions;
	
	public bool Enabled;

	public List<MidiSchedulerComponent> Components = new();
	
	public void InitializePerformance()
	{
		// Get path of standard
		var init = GetNode("/root/PerformanceScreenInit");

		// Initialize based on notes path
		var notesPath = (string)init.Get("notes_path");
		if (notesPath == "")
			InitializeTradingPerformance(init);
		else
			InitializePlaybackPerformance(init);
		
		// Add metronome
		if (StartMeasure != 0) return;
		
		AddMeasure(-2, new MidiMeasure([
			new MidiNote(OutputName.Metronome, 0.0, 0.2, 22, 48),
			new MidiNote(OutputName.Metronome, 0.5, 0.2, 22, 48),
		]));
		
		AddMeasure(-1, new MidiMeasure([
			new MidiNote(OutputName.Metronome, 0.00, 0.2, 22, 48),
			new MidiNote(OutputName.Metronome, 0.25, 0.2, 22, 48),
			new MidiNote(OutputName.Metronome, 0.50, 0.2, 22, 48),
			new MidiNote(OutputName.Metronome, 0.75, 0.2, 22, 48),
		]));
	}

	public void InitializeTradingPerformance(Node init)
	{
		var standardName = (string)init.Get("standard_name");
		var standardPath = $"user://saves/{standardName}/";

		var soloistIndex = (int)init.Get("soloist");
		
		// Load necessary files
		var backingTrack = MidiSong.FromNotesFileStream(new FileAccessStream(standardPath + "backing.notes", Read));
		var soloTrack = MidiSong.FromNotesFileStream(new FileAccessStream(standardPath + "solo.notes", Read));
		var leadSheet = LeadSheet.FromStream(new FileAccessStream(standardPath + "sheet.json", Read));
		
		// Get BPM, apply to MidiRecorder song
		Bpm = leadSheet.Bpm;
		Recorder.Song.Bpm = Bpm;
		
		// Add components
		SongLength = backingTrack.Measures.Count;
		Repetitions = (int)init.Get("repetition_count");
		Components.Add(new SongMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = Recorder,
			Song = backingTrack,
			Repetitions = Repetitions,
		});

		Soloist soloist = soloistIndex switch
		{
			0 => new NoteRandomSoloist(),
			1 => new RetrievalSoloist(),
			2 => new NoteFactorOracleSoloist(),
			3 => new TokenRandomSoloist(),
			4 => new TokenFactorOracleSoloist(), 
			5 => new TokenMarkovSoloist(standardPath),
			6 => new TokenShuffleSoloist(),
			7 => new TokenTransformerSoloist(),
			_ => throw new ArgumentOutOfRangeException()
		};
        
		Components.Add(new SoloMidiSchedulerComponent(soloTrack, leadSheet, soloist)
		{
			Scheduler = this,
			Recorder = Recorder,
			Repetitions = Repetitions
		});
	}

	public void InitializePlaybackPerformance(Node init)
	{
		var notesPath = (string)init.Get("notes_path");
		var startMeasure = (int)init.Get("start_measure");
		
		// Disable recording
		// TODO: might want to leave that on?
		Recorder.Active = false;
		
		// Load track
		var track = MidiSong.FromNotesFileStream(new FileAccessStream(notesPath, Read));
		
		// Get BPM, apply to MidiRecorder song
		Bpm = track.Bpm;
		Recorder.Song.Bpm = Bpm;
        
		// Add component
		SongLength = 32;
		Repetitions = 2;
		
		Components.Add(new SongMidiSchedulerComponent
		{
			Scheduler = this,
			Recorder = Recorder,
			Song = track,
			Repetitions = Repetitions,
		});

		StartMeasure = startMeasure;
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
		var currentMeasure = currentTimeMs / 1000.0 / (60.0 / Bpm) / 4.0;
		
		return currentMeasure - 2 + StartMeasure;
	}
    
	public void Tick(double currentTime)
	{
		// Call components every measure
		var currentMeasure = (int)Math.Truncate(currentTime);
		if ((int)Math.Truncate(Time) != currentMeasure)
		{
			if (currentMeasure == 1 + SongLength * Repetitions)
			{
				Recorder.CallDeferred("Save");
				((PerformanceScreen)GetTree().CurrentScene).CallDeferred("Quit");
				
				Enabled = false;
				return;
			}
			
			foreach (var component in Components)
				component.HandleMeasure(currentMeasure);
		}
		
		// Update current time
		Time = currentTime;
		
		// Play notes when needed
		lock (NoteQueue)
		{
			while (NoteQueue.TryPeek(out _, out var time) && time < Time)
			{
				var note = NoteQueue.Dequeue();
				MidiServer.Instance.Send(note);
			}
		}
	}

	public void AddMeasure(int measureNum, MidiMeasure measure)
	{
		lock (NoteQueue)
		{
			foreach (var note in measure.Notes)
			{
				if (note.Length <= 0.0) continue;
				if (note.Velocity == 0) continue;
				
				var newNoteTime = note.Time + measureNum;
				NoteQueue.Enqueue(note with { Time = newNoteTime }, newNoteTime);

				var noteOffTime = newNoteTime + note.Length;

				NoteQueue.Enqueue(new MidiNote(note.OutputName, noteOffTime, 0, note.Note, 0), noteOffTime);
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