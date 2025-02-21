using System;
using System.IO;
using Core.midi;
using Core.midi.token;
using Godot;
using Program.midi.scheduler;
using Program.midi.scheduler.component;

public partial class TokenLoader : Node
{
	public override void _Ready()
	{
		var scheduler = (MidiScheduler)GetNode("%MidiScheduler");
		
		scheduler.BPM = 120;
		scheduler.SongLength = 1;
		scheduler.Repetitions = 9999;
		
		scheduler.Components.Add(new MetronomeMidiSchedulerComponent
		{
			Scheduler = scheduler,
			Recorder = null,
		});
        
		scheduler.Start();
	}

	public void LoadTokens(string filePath)
	{
		var text = File.ReadAllText(filePath);
		var tokens = TokenMethods.TokensFromString(text);
		
		// Get song from notes
		var leadSheet = new LeadSheet { Chords = [[Chord.CMajor]] };
		var notes = TokenMethods.Reconstruct(tokens, leadSheet, 0);
		var song = MidiSong.FromNotes(notes);

		var scheduler = (MidiScheduler)GetNode("%MidiScheduler");
		var currentMeasure = (int)Math.Truncate(scheduler.Time);
		scheduler.AddSong(currentMeasure + 1, song);

		var tokensStr = TokenMethods.TokensToString(tokens);
		((TextEdit)GetNode("%SelectedTokensText")).Text = "Tokens found:\n" + tokensStr.Replace("M", "M\n");

		((Button)GetNode("%StopPlayingButton")).Visible = true;
	}

	public void StopPlaying()
	{
		var scheduler = (MidiScheduler)GetNode("%MidiScheduler");
		scheduler.NoteQueue.Clear();

		((TextEdit)GetNode("%SelectedTokensText")).Text = "Tokens found:\n(...)";
		
		((Button)GetNode("%StopPlayingButton")).Visible = false;
	}
}
