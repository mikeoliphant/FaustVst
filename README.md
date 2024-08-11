# FaustVst

FaustVst is a VST3 plugin that allows you to dynamically load/compile/edit [Faust](https://faust.grame.fr/) effects from source dsp files.

It is lets you do quick iteration (make a change and reload it nearly instantly), all while running integrated in your DAW.

Here is an example of a simple gain/pan plugin with output level meters:

![GainPan](https://github.com/user-attachments/assets/c39db00f-8691-4125-b5bd-62b5339eab95)

and this is the source Faust dsp code that generated it:

```
import("stdfaust.lib");

gain_pan(x, y) = sp.constantPowerPan(pan, x, y) : gainxy
with {
        gainxy(x, y) = x * gain, y * gain;
        gain = vslider("[0] Gain [unit:dB] [style:knob]", 0, -40, 40, 1) : ba.db2linear;
        pan = vslider("[1] Pan [style:knob]", 0.5, 0, 1, .01);
};

left_meter(x)	= attach(x, ba.linear2db(x) : vbargraph("Left [unit:dB]", -96, 10));
right_meter(x)	= attach(x, ba.linear2db(x) : vbargraph("Right [unit:dB]", -96, 10));

process = hgroup("Stereo Gain/Pan", hgroup("Gain/Pan", gain_pan) : hgroup("Output", (left_meter, right_meter)));
```

# Performance

FaustVst is intended to provide a quick-iteration framework for working on Faust effects. While it does produce relatively well optimized code that is suitable for realtime testing, performance will be not as good as a compiled C++ effect. 

# How does it work?

FaustVst uses the faust compiler to create C# code, which is then dynamically compiled into an assembly and run in the plugin (using [AudioPlugSharp](https://github.com/mikeoliphant/AudioPlugSharp)).

The plugin UI is done using [UILayout](https://github.com/mikeoliphant/UILayout), a lightweight UI library that runs on top of [MonoGame](https://github.com/MonoGame/MonoGame).


