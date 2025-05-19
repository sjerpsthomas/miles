using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.algorithm;
using Core.algorithm.tokens_v1;
using Core.algorithm.tokens_v2;
using Core.midi;
using Core.tokens.v2.conversion;
using Core.tokens.v2.conversion.stage;
using Godot;
using Program.midi.recorder;
using Program.midi.scheduler.component;
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
	public int StartMeasure;
	
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
		
		// Load lead sheet
		var leadSheet = LeadSheet.FromStream(new FileAccessStream(standardPath + "sheet.json", Read));

		// Load music data
		var backingTrack = LoadSong("backing.notes");
		MidiSong[] solos =
		[
			LoadSong("solo.notes"),
			..Enumerable.Range(1, 4).Select(i => LoadSong($"_extra_{i}.notes"))
		];
		
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

		IAlgorithm algorithm = soloistIndex switch
		{
			// 0 => new NoteRandomSoloist(),
			0 => new V2_TokenReplayAlgorithm(),
			1 => new RetrievalAlgorithm(),
			2 => new NoteFactorOracleAlgorithm(),
			3 => new V1_TokenRandomAlgorithm(),
			4 => new V1_TokenFactorOracleAlgorithm(), 
			5 => new V1_TokenMarkovAlgorithm(),
			6 => new V1_TokenShuffleAlgorithm(),
			7 => new V1_TokenTransformerAlgorithm(),
			8 => new V2_TokenFactorOracleAlgorithm(),
			9 => new V2_TokenMarkovAlgorithm(),
			10 => new V2_NoteRepMarkovAlgorithm(),
			11 => new V2_TokenNeuralNetAlgorithm(),
			_ => throw new ArgumentOutOfRangeException()
		};
        
		Components.Add(new AlgorithmMidiSchedulerComponent(solos, leadSheet, algorithm)
		{
			Scheduler = this,
			Recorder = Recorder,
			Repetitions = Repetitions
		});
		return;

		// (Loads a MidiSong from the specified path)
		MidiSong LoadSong(string path) =>
			MidiSong.FromNotesFileStream(new FileAccessStream(standardPath + path, Read));
	}

	public void InitializePlaybackPerformance(Node init)
	{
		var standardName = (string)init.Get("standard_name");
		var standardPath = $"user://saves/{standardName}/";
		
		var notesPath = (string)init.Get("notes_path");
		var startMeasure = (int)init.Get("start_measure");
		
		// Disable recording
		// TODO: might want to leave that on?
		Recorder.Active = false;
		
		// Load track
		var track = MidiSong.FromNotesFileStream(new FileAccessStream("res://recordings/" + notesPath, Read));
		var leadSheet = LeadSheet.FromStream(new FileAccessStream(standardPath + "sheet.json", Read));
		
		// Get BPM, apply to MidiRecorder song
		Bpm = track.Bpm;
		Recorder.Song.Bpm = Bpm;
        
		SongLength = leadSheet.Chords.Count;
		Repetitions = 2;
		
		// Add component
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