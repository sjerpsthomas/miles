using System;
using System.Collections.Generic;
using Godot;
using NAudio.Midi;
using static thesis.midi.scheduler.MidiScheduler;

namespace thesis.midi;

public partial class MidiServer : Node
{
    public static MidiServer Instance;
    
    public MidiIn LearnerIn;

    public enum OutputName
    {
        Loopback = 0,
        Algorithm,
    }

    public Dictionary<OutputName, MidiOut> Outputs;

    public delegate void NoteSentHandler(OutputName outputName, NoteData note);

    public event NoteSentHandler NoteSent;
    
    public override void _Ready()
    {
        Instance = this;

        LearnerIn = FindMidiIn("LKMK3 MIDI");
        LearnerIn.Start();
        
        Outputs = new()
        {
            [OutputName.Loopback] = FindMidiOut("Learner"), // TODO rename
            [OutputName.Algorithm] = FindMidiOut("Algorithm")
        };

        GD.Print("[MIDI] Setup successful!");

        LearnerIn.MessageReceived += (_, args) =>
        {
            if (args.MidiEvent is not NoteEvent noteEvent) return;

            var note = new NoteData(-1.0, -1.0, noteEvent.NoteNumber, noteEvent.Velocity);
            Send(OutputName.Loopback, note);
        };
    }

    public void Send(OutputName outputName, NoteData note)
    {
        var noteEvent = new NoteOnEvent(0, 1, note.Note, note.Velocity, 0);
        Outputs[outputName].Send(noteEvent.GetAsShortMessage());

        NoteSent!(outputName, note);
    }
    
    public void Send(OutputName outputName, int note, int velocity) =>
        Send(outputName, new NoteData(0.0, 0.0, note, velocity));
    
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
