# Iodine Programming Language
[![Build Status](https://travis-ci.org/IodineLang/Iodine.svg)](https://travis-ci.org/IodineLang/Iodine)

Iodine is dynamically typed multi-paradigm programming language written in C#. The syntax of the Iodine is derived from several languages including Python, C#, and F#

#### Compiling
Iodine requires either .NET or mono to run.

Iodine can be compiled on *NIX systems by running ```make``` command. 

#### Installation
Iodine can be installed on *NIX systems by running ```make install``` as root. 

At the moment there is no installer for Windows, however, iodine should compile fine on Windows and can be used by manually running iodine.exe

#### Usage
A file can be ran by invoking the interpreter as such
```
iodine myFile.id
```

#### Example
Below is a Hello, World program in Iodine. You can find more examples in the examples directory
```go
print ("Hello, World!")
```
