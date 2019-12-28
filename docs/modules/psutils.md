## ```psutils``` module

Provides a portable way for interacting with and creating processes
### Functions

#### func ```popen``` (commmand, mode)
___
Opens up a new process, returning a Proc object.
#### func ```spawn``` (executable, [args], [wait])
___
Spawns a new process.

#### class ```Process``` ()
___
An active processNote: This class cannot be instantiated directly
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|kill| **None** |Attempts to kill the associated process.|


#### class ```Subprocess``` ()
___
A subprocess spawned from ```psutils.popen```**Note**: This class cannot be instantiated directly
##### Methods

| Name                    | Arguments         | Description |
| ----------------------- | ----------------- | ----------- |
|readln| **None** |Reads a single line from the process's standard output stream.|
|alive| **None** |Returns true if the process is alive.|
|writeln| *args |Writes each string passed in *args to the process's standard input stream and appends a new line|
|empty| **None** |Returns true if there is no more data to be read from stdout.|
|write| *args |Writes each string passed in *args to the process's standard input stream|
|kill| **None** |Attempts to kill the associated process.|
|read| **None** |Reads all text written to the process's standard output stream.|



