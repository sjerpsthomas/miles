using Godot;
using System;
using System.Collections.Generic;

public partial class MidiOutServer : Node
{
	
    
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

public class MidiOutHandler
{
	public bool Enabled;

	public float BPM;
	public float MeasureTime => 4f / (BPM * 60f);
	
	public int StartTime;

	public float CurrentTime;
	public float CurrentMeasureTime => throw new NotImplementedException("todo");
	public float CurrentMeasure => throw new NotImplementedException("todo");
	
	public PriorityQueue<NoteData, float> NoteQueue;
    
	public void Run()
	{
		while (Enabled)
			Tick();
	}

	private void Tick()
	{
		// Compute stuff
		
		// Play notes when needed
		lock (NoteQueue)
		{
			while (NoteQueue.TryPeek(out var note, out var time) && time < CurrentTime + 0.01f)
				PlayNote(note);
		}
		
		// Wait a bit
	}

	public void PlayNote(NoteData note)
	{
		// TODO
	}
	
	public void AddMeasure(int measure, MeasureData data)
	{
		var measureTime = GetTimeFromMeasure(measure);
		
		lock (NoteQueue)
		{
			foreach (var note in data.Notes)
				NoteQueue.Enqueue(note, note.RelativeTime + measureTime);
		}
	}

	public int GetMeasureFromTime(float time) => throw new NotImplementedException("todo");
	public float GetTimeFromMeasure(int measure) => throw new NotImplementedException("todo");
}

public class NoteData
{
	public float RelativeTime;
	
	public int Note;
	public int Velocity;
	public int Instrument;
}

public class MeasureData
{
	public List<NoteData> Notes;
}

public abstract class MidiOutComponent
{
	public abstract void Execute(int measure);
}