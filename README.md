# FaustVst

FaustVst is a VST3 plugin that allows you to dynamically load/compile/edit [Faust](https://github.com/grame-cncm/faust) dsp files while running integrated in your DAW.

FaustVst is intended to provide a quick-iteration framework for working on Faust effects. While it does produce relatively well optimized code that is suitable for realtime testing, performance will be not as good as a compiled C++ effect. 

# How does it work?

FaustVst uses the faust compiler to create C# code, which is then dynamically compiled into an assembly and run in the plugin (using [AudioPlugSharp](https://github.com/mikeoliphant/AudioPlugSharp)).

The plugin UI is done using [UILayout](https://github.com/mikeoliphant/UILayout), a lightweight UI library that runs on top of [MonoGame](https://github.com/MonoGame/MonoGame).


