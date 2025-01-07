using System;
using Godot;
using Godot.Collections;
using NAudio.Midi;

namespace thesis.midi;

// ReSharper disable once UnusedType.Global
public partial class MidiServer : Node2D
{
	public MidiOut LearnerOut;
	public MidiOut AlgorithmOut;
	public MidiIn LearnerIn;

	public const int KeyCount = 61;
	
	public Array<bool> Keys;

	[Signal]
	public delegate void NoteReceivedEventHandler(int note, int velocity);
	
	public override void _Ready()
	{
		Keys = new Array<bool>();
		Keys.Resize(61);
		Keys.Fill(false);
		
		LearnerOut = FindMidiOut("Learner");
		AlgorithmOut = FindMidiOut("Algorithm");
		
		LearnerIn = FindMidiIn("LKMK3 MIDI");
		LearnerIn.Start();

		GD.Print("[MIDI] Setup successful!");

		LearnerIn.MessageReceived += (_, args) =>
		{
			if (args.MidiEvent is not NoteEvent noteEvent) return;

			ShowNoteEvent(noteEvent);
			LearnerOut.Send(noteEvent.GetAsShortMessage());
		};
	}

	public void Send(string output, int noteNumber, int velocity)
	{
		var midiOutput = output switch
		{
			"Algorithm" => AlgorithmOut,
			"Learner" => LearnerOut,
			_ => throw new ArgumentException($"Unknown output {output}!")
		};
		
		var noteEvent = new NoteOnEvent(0, 1, noteNumber, velocity, 0);
		midiOutput.Send(noteEvent.GetAsShortMessage());
		ShowNoteEvent(noteEvent);
	}

	public string GetHeldPattern()
	{
		var chars = new char[12];
		for (var i = 0; i < 12; i++)
			chars[i] = '0';
		
		for (var i = 0; i < KeyCount; i++)
			if (Keys[i]) chars[i % 12] = '1';

		return string.Concat(chars);
	}
	
	private void ShowNoteEvent(NoteEvent noteEvent)
	{
		var note = noteEvent.NoteNumber;
		var pressed = noteEvent.Velocity > 0;

		// Remap from keyboard
		note -= 36;

		if (note is < 0 or > 60)
			return;
        
		Keys[note] = pressed;
	}
	
	private MidiOut FindMidiOut(string name)
	{
		for (var i = 0; i < MidiOut.NumberOfDevices; i++)
			if (MidiOut.DeviceInfo(i).ProductName == name)
				return new MidiOut(i);

		throw new ArgumentException($"Cannot find MIDI output {name}");
	}

	private MidiIn FindMidiIn(string name)
	{
		for (var i = 0; i < MidiIn.NumberOfDevices; i++)
			if (MidiIn.DeviceInfo(i).ProductName == name)
				return new MidiIn(i);

		throw new ArgumentException($"Cannot find MIDI input {name}");
	}
}