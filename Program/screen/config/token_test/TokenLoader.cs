using System;
using System.IO;
using Core.midi;
using Core.tokens.v1;
using Godot;
using Program.midi.scheduler;
using Program.midi.scheduler.component;

namespace Program.screen.config.token_test;

public partial class TokenLoader : Node
{
	public MidiScheduler Scheduler => (MidiScheduler)GetNode("%MidiScheduler");
	public TextEdit SelectedTokensText => (TextEdit)GetNode("%SelectedTokensText");
	
	public override void _Ready()
	{
		Scheduler.Bpm = 120;
		Scheduler.SongLength = 1;
		Scheduler.Repetitions = 9999;
		
		Scheduler.Components.Add(new MetronomeMidiSchedulerComponent
		{
			Scheduler = Scheduler,
			Recorder = null,
		});
        
		Scheduler.Start();
	}

	public void LoadFile(string filePath)
	{
		var extension = Path.GetExtension(filePath);
		switch (extension)
		{
			case ".tokens":
				LoadTokens(filePath);
				break;
			case ".notes":
				LoadNotes(filePath);
				break;
			case ".mid":
				LoadMidi(filePath);
				break;
			default:
				SelectedTokensText.Text += $"Unknown file format at {filePath}!";
				break;
		}
	}

	public void LoadTokens(string filePath)
	{
		// Open file
		var text = File.ReadAllText(filePath);
		var tokens = V1_TokenMethods.V1_TokensFromString(text);
		
		// Get song from notes
		var leadSheet = new LeadSheet { Chords = [[Chord.CMajor]] };
		var notes = V1_TokenMethods.V1_Reconstruct(tokens, leadSheet, 0);
		var song = MidiSong.FromNotes(notes);

		var scheduler = (MidiScheduler)GetNode("%MidiScheduler");
		var currentMeasure = (int)Math.Truncate(scheduler.Time);
		scheduler.AddSong(currentMeasure + 1, song);

		// Log
		var tokensStr = V1_TokenMethods.V1_TokensToString(tokens);
		SelectedTokensText.Text = $"Tokens from {filePath} loaded successfully!\n{tokensStr.Replace("M", "M\n")}";

		// Show 'stop playing' button
		((Button)GetNode("%StopPlayingButton")).Visible = true;
	}

	public void LoadNotes(string filePath)
	{
		// Get song
		var song = MidiSong.FromNotesFileStream(new FileStream(filePath, FileMode.Open));
		
		// Schedule song
		var scheduler = (MidiScheduler)GetNode("%MidiScheduler");
		var currentMeasure = (int)Math.Truncate(scheduler.Time);
		scheduler.AddSong(currentMeasure + 1, song);
		
		// Log
		SelectedTokensText.Text = $"Notes from {filePath} loaded successfully!\n";
        
		// Show 'stop playing' button
		((Button)GetNode("%StopPlayingButton")).Visible = true;
	}

	public void LoadMidi(string filePath)
	{
		// Get song
		var song = MidiSong.FromMidiFileStream(new FileStream(filePath, FileMode.Open));
		
		// Schedule song
		var scheduler = (MidiScheduler)GetNode("%MidiScheduler");
		var currentMeasure = (int)Math.Truncate(scheduler.Time);
		scheduler.AddSong(currentMeasure + 1, song);
		
		// Log
		SelectedTokensText.Text = $"MIDI from {filePath} loaded successfully!\n";
        
		// Show 'stop playing' button
		((Button)GetNode("%StopPlayingButton")).Visible = true;
	}

	public void Stop()
	{
		Scheduler.Stop();
		Scheduler.Start();

		SelectedTokensText.Text += "Stopped playing\n";
		
		((Button)GetNode("%StopPlayingButton")).Visible = false;
	}

	public void Quit()
	{
		Scheduler.Stop();
	}
}